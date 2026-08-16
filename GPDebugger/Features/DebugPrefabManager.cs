using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using TextToy = LabApi.Features.Wrappers.TextToy;

namespace GPDebugger.Features
{
    public static class DebugPrefabManager
    {
        private static readonly Dictionary<int, GameObject> SpawnedPrefabs = new();
        private static readonly Dictionary<int, TextToy> SpawnedLabels = new();
        private static int _nextId = 1;
        private static CoroutineHandle _lineupCoroutine;

        public static bool IsLineupRunning { get; private set; }

        public static bool Spawn(Player player, string prefabName, out string response)
        {
            if (player == null || player.CameraTransform == null)
            {
                response = "Player camera is unavailable.";
                return false;
            }

            if (!TryResolvePrefabType(prefabName, out PrefabType prefabType, out int enumIndex))
            {
                response = $"Unknown PrefabType name or enum index '{prefabName}'. Use 'gpdebug prefab list {prefabName}' to search.";
                return false;
            }

            if (prefabType == PrefabType.Player)
            {
                response = "PrefabType.Player cannot be spawned by GPDebugger.";
                return false;
            }

            Transform camera = player.CameraTransform;
            Ray ray = new Ray(camera.position + camera.forward * 0.2f, camera.forward);
            float maxDistance = Math.Max(1f, Main.Instance?.Config.PointerInspectorMaxDistance ?? 200f);
            Vector3 position;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, Physics.AllLayers, QueryTriggerInteraction.Collide))
                position = hit.point + hit.normal * 0.02f;
            else
                position = ray.GetPoint(3f);

            Quaternion rotation = Quaternion.Euler(0f, camera.eulerAngles.y, 0f);

            try
            {
                GameObject spawned = PrefabHelper.Spawn(prefabType, position, rotation);
                if (spawned == null)
                {
                    response = $"PrefabType.{prefabType} is not registered in the current server build.";
                    return false;
                }

                int id = _nextId++;
                SpawnedPrefabs[id] = spawned;
                response =
                    $"Spawned PrefabType.{prefabType} (enum index {enumIndex}) with GPDebugger ID {id}.\n" +
                    $"Position: {FormatVector3(position)}\n" +
                    $"Rotation Y: {rotation.eulerAngles.y.ToString("0.###", CultureInfo.InvariantCulture)}";
                return true;
            }
            catch (Exception exception)
            {
                response = $"Failed to spawn PrefabType.{prefabType}: {exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        public static bool RemoveLookTarget(Player player, out string response)
        {
            if (player == null || player.CameraTransform == null)
            {
                response = "Player camera is unavailable.";
                return false;
            }

            CleanupDestroyedEntries();
            Transform camera = player.CameraTransform;
            Ray ray = new Ray(camera.position + camera.forward * 0.2f, camera.forward);
            float maxDistance = Math.Max(1f, Main.Instance?.Config.PointerInspectorMaxDistance ?? 200f);
            if (!TransformInspector.TrySelectTarget(ray, maxDistance, out GameObject target, out _))
            {
                response = "No object found under the crosshair.";
                return false;
            }

            Transform current = target.transform;
            while (current != null)
            {
                KeyValuePair<int, GameObject> match = SpawnedPrefabs.FirstOrDefault(pair => pair.Value != null && pair.Value == current.gameObject);
                if (match.Value != null)
                    return Remove(match.Key, out response);

                current = current.parent;
            }

            response = "The object under the crosshair was not spawned by GPDebugger.";
            return false;
        }

        public static bool Remove(int id, out string response)
        {
            CleanupDestroyedEntries();
            if (!SpawnedPrefabs.TryGetValue(id, out GameObject spawned))
            {
                response = $"GPDebugger prefab ID {id} was not found.";
                return false;
            }

            string name = spawned == null ? "destroyed object" : spawned.name;
            Destroy(spawned);
            DestroyLabel(id);
            SpawnedPrefabs.Remove(id);
            response = $"Removed GPDebugger prefab ID {id}: {name}.";
            return true;
        }

        public static bool StartLineup(Player player, float spacing, out string response)
        {
            if (player == null || player.CameraTransform == null)
            {
                response = "Player camera is unavailable.";
                return false;
            }

            if (IsLineupRunning)
            {
                response = "A prefab lineup is already being spawned. Use 'gpdebug prefab remove all' to cancel and remove it.";
                return false;
            }

            if (float.IsNaN(spacing) || float.IsInfinity(spacing) || spacing < 1f || spacing > 50f)
            {
                response = "Prefab lineup spacing must be between 1 and 50 metres.";
                return false;
            }

            Transform camera = player.CameraTransform;
            Ray ray = new Ray(camera.position + camera.forward * 0.2f, camera.forward);
            float maxDistance = Math.Max(1f, Main.Instance?.Config.PointerInspectorMaxDistance ?? 200f);
            Vector3 origin = Physics.Raycast(ray, out RaycastHit hit, maxDistance, Physics.AllLayers, QueryTriggerInteraction.Collide)
                ? hit.point + hit.normal * 0.02f
                : ray.GetPoint(5f);

            float snappedYaw = Mathf.Repeat(Mathf.Round(camera.eulerAngles.y / 90f) * 90f, 360f);
            Quaternion prefabRotation = Quaternion.Euler(0f, snappedYaw, 0f);
            Vector3 lineDirection = prefabRotation * Vector3.right;
            Quaternion labelRotation = prefabRotation * Quaternion.Euler(0f, 180f, 0f);
            PrefabType[] prefabs = GetOrderedPrefabTypes()
                .Where(prefabType => prefabType != PrefabType.Player)
                .ToArray();

            IsLineupRunning = true;
            _lineupCoroutine = Timing.RunCoroutine(LineupCoroutine(
                player.UserId,
                prefabs,
                origin,
                lineDirection,
                prefabRotation,
                labelRotation,
                spacing));

            response =
                $"Started prefab lineup: {prefabs.Length} prefab type(s), spacing {spacing.ToString("0.###", CultureInfo.InvariantCulture)} m.\n" +
                $"Origin: {FormatVector3(origin)}\n" +
                $"Snapped Y rotation: {snappedYaw.ToString("0", CultureInfo.InvariantCulture)} degrees.\n" +
                "Prefabs are spawned one per frame. Use 'gpdebug prefab remove all' to cancel and remove the lineup.";
            return true;
        }

        public static int DestroyAll()
        {
            IsLineupRunning = false;
            if (_lineupCoroutine.IsRunning)
                Timing.KillCoroutines(_lineupCoroutine);

            int count = 0;
            foreach (GameObject spawned in SpawnedPrefabs.Values.ToArray())
            {
                if (spawned == null)
                    continue;

                Destroy(spawned);
                count++;
            }

            foreach (TextToy label in SpawnedLabels.Values.ToArray())
            {
                try
                {
                    label?.Destroy();
                }
                catch (Exception exception)
                {
                    Log.Debug($"Could not destroy prefab label: {exception.Message}");
                }
            }

            SpawnedPrefabs.Clear();
            SpawnedLabels.Clear();
            _nextId = 1;
            _lineupCoroutine = default;
            return count;
        }

        public static string BuildPrefabList(string filter)
        {
            KeyValuePair<int, PrefabType>[] entries = GetOrderedPrefabTypes()
                .Select((prefabType, index) => new KeyValuePair<int, PrefabType>(index, prefabType))
                .ToArray();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                entries = entries
                    .Where(entry => entry.Key.ToString(CultureInfo.InvariantCulture) == filter ||
                                    entry.Value.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
            }

            if (entries.Length == 0)
                return $"No PrefabType name contains '{filter}'.";

            return $"PrefabType results ({entries.Length}):\n- " +
                   string.Join("\n- ", entries.Select(entry => $"{entry.Key}: {entry.Value}"));
        }

        private static bool TryResolvePrefabType(string input, out PrefabType prefabType, out int enumIndex)
        {
            PrefabType[] ordered = GetOrderedPrefabTypes();
            if (int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out int requestedIndex))
            {
                if (requestedIndex >= 0 && requestedIndex < ordered.Length)
                {
                    prefabType = ordered[requestedIndex];
                    enumIndex = requestedIndex;
                    return true;
                }

                prefabType = default;
                enumIndex = -1;
                return false;
            }

            if (Enum.TryParse(input, true, out prefabType) && Enum.IsDefined(typeof(PrefabType), prefabType))
            {
                enumIndex = Array.IndexOf(ordered, prefabType);
                return enumIndex >= 0;
            }

            prefabType = default;
            enumIndex = -1;
            return false;
        }

        private static PrefabType[] GetOrderedPrefabTypes()
        {
            return Enum.GetValues(typeof(PrefabType))
                .Cast<PrefabType>()
                .OrderBy(prefabType => Convert.ToInt64(prefabType, CultureInfo.InvariantCulture))
                .ToArray();
        }

        private static void CleanupDestroyedEntries()
        {
            foreach (int id in SpawnedPrefabs.Where(pair => pair.Value == null).Select(pair => pair.Key).ToArray())
            {
                DestroyLabel(id);
                SpawnedPrefabs.Remove(id);
            }
        }

        private static IEnumerator<float> LineupCoroutine(
            string requestingUserId,
            PrefabType[] prefabTypes,
            Vector3 origin,
            Vector3 lineDirection,
            Quaternion prefabRotation,
            Quaternion labelRotation,
            float spacing)
        {
            int spawnedCount = 0;
            int failedCount = 0;

            for (int index = 0; index < prefabTypes.Length && IsLineupRunning; index++)
            {
                PrefabType prefabType = prefabTypes[index];
                Vector3 position = origin + lineDirection * (index * spacing);

                try
                {
                    GameObject spawned = PrefabHelper.Spawn(prefabType, position, prefabRotation);
                    if (spawned == null)
                    {
                        failedCount++;
                    }
                    else
                    {
                        MakeDisplayStatic(spawned);

                        int id = _nextId++;
                        SpawnedPrefabs[id] = spawned;

                        float labelHeight = GetLabelHeight(spawned, position);
                        TextToy label = CreateLabel(
                            position + Vector3.up * labelHeight,
                            labelRotation,
                            $"<size=6><color=#7FDBFF>{index + 1}</color>  <color=white>{prefabType}</color></size>");
                        if (label != null)
                            SpawnedLabels[id] = label;

                        spawnedCount++;
                    }
                }
                catch (Exception exception)
                {
                    failedCount++;
                    Log.Error($"Failed to add PrefabType.{prefabType} to lineup: {exception}");
                }

                yield return Timing.WaitForOneFrame;
            }

            bool completed = IsLineupRunning;
            IsLineupRunning = false;
            _lineupCoroutine = default;

            if (!completed)
                yield break;

            string message = $"Prefab lineup complete: {spawnedCount} spawned, {failedCount} failed.";
            Log.Info(message);
            Player player = Player.Get(requestingUserId);
            if (player != null)
                player.RemoteAdminMessage(message, failedCount == 0, "GPDebugger");
        }

        private static TextToy CreateLabel(Vector3 position, Quaternion rotation, string text)
        {
            TextToy label = TextToy.Create();
            if (label == null)
                return null;

            label.Position = position;
            label.Rotation = rotation;
            label.DisplaySize = new Vector2(100000f, 100000f);
            label.TextFormat = text;
            label.SyncInterval = 0f;
            return label;
        }

        private static void MakeDisplayStatic(GameObject spawned)
        {
            foreach (Rigidbody body in spawned.GetComponentsInChildren<Rigidbody>(true))
            {
                if (body == null)
                    continue;

                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.constraints = RigidbodyConstraints.FreezeAll;
                body.isKinematic = true;
            }
        }

        private static float GetLabelHeight(GameObject spawned, Vector3 origin)
        {
            bool hasBounds = false;
            Bounds combinedBounds = default;

            foreach (Renderer renderer in spawned.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                foreach (Collider collider in spawned.GetComponentsInChildren<Collider>(true))
                {
                    if (collider == null)
                        continue;

                    if (!hasBounds)
                    {
                        combinedBounds = collider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(collider.bounds);
                    }
                }
            }

            float height = hasBounds ? combinedBounds.max.y - origin.y + 0.75f : 2.5f;
            return Mathf.Clamp(height, 2.5f, 15f);
        }

        private static void DestroyLabel(int id)
        {
            if (!SpawnedLabels.TryGetValue(id, out TextToy label))
                return;

            try
            {
                label?.Destroy();
            }
            catch (Exception exception)
            {
                Log.Debug($"Could not destroy prefab label {id}: {exception.Message}");
            }

            SpawnedLabels.Remove(id);
        }

        private static void Destroy(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            NetworkIdentity identity = gameObject.GetComponent<NetworkIdentity>();
            if (NetworkServer.active && identity != null)
                NetworkServer.Destroy(gameObject);
            else
                UnityEngine.Object.Destroy(gameObject);
        }

        private static string FormatVector3(Vector3 value)
        {
            return
                $"({value.x.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                $"{value.y.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                $"{value.z.ToString("0.###", CultureInfo.InvariantCulture)})";
        }
    }
}
