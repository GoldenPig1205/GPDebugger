using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using GPDebugger.Commands.GPDebugger;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Extension;
using MEC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using HsmHint = HintServiceMeow.Core.Models.Hints.Hint;

namespace GPDebugger.Features
{
    public static class TransformInspector
    {
        private const string HintId = "GPDebugger.PointerTransformInspector";

        private static readonly HashSet<string> EnabledUsers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, CoroutineHandle> Coroutines = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, HsmHint> DisplayedHints = new(StringComparer.OrdinalIgnoreCase);

        private static Renderer[] _cachedRenderers = Array.Empty<Renderer>();
        private static Transform[] _cachedTransforms = Array.Empty<Transform>();
        private static DateTime _sceneCacheExpiresAt = DateTime.MinValue;
        private static bool _isRegistered;

        public static void Register()
        {
            if (_isRegistered)
                return;

            Exiled.Events.Handlers.Player.Left += OnLeft;
            _isRegistered = true;
        }

        public static void Unregister()
        {
            if (_isRegistered)
            {
                Exiled.Events.Handlers.Player.Left -= OnLeft;
                _isRegistered = false;
            }

            StopAll();
        }

        public static bool Start(Player player)
        {
            if (player == null || player.IsNPC || string.IsNullOrWhiteSpace(player.UserId))
                return false;

            if (!EnabledUsers.Add(player.UserId))
                return false;

            StopCoroutine(player.UserId);
            Coroutines[player.UserId] = Timing.RunCoroutine(InspectLoop(player.UserId));
            return true;
        }

        public static bool Stop(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.UserId))
                return false;

            bool wasEnabled = EnabledUsers.Remove(player.UserId);
            StopCoroutine(player.UserId);
            RemoveHint(player);
            return wasEnabled;
        }

        public static void StopAll()
        {
            foreach (CoroutineHandle coroutine in Coroutines.Values.ToArray())
            {
                if (coroutine.IsRunning)
                    Timing.KillCoroutines(coroutine);
            }

            foreach (string userId in DisplayedHints.Keys.ToArray())
            {
                Player player = Player.Get(userId);
                if (player != null)
                    RemoveHint(player);
            }

            EnabledUsers.Clear();
            Coroutines.Clear();
            DisplayedHints.Clear();
            _cachedRenderers = Array.Empty<Renderer>();
            _cachedTransforms = Array.Empty<Transform>();
            _sceneCacheExpiresAt = DateTime.MinValue;
        }

        private static IEnumerator<float> InspectLoop(string userId)
        {
            while (EnabledUsers.Contains(userId))
            {
                Player player = Player.Get(userId);
                if (player == null || player.CameraTransform == null)
                {
                    yield return Timing.WaitForSeconds(GetUpdateInterval());
                    continue;
                }

                try
                {
                    RenderCurrentTarget(player);
                }
                catch (Exception exception)
                {
                    Render(player, BuildErrorText(exception));
                }

                yield return Timing.WaitForSeconds(GetUpdateInterval());
            }
        }

        private static void RenderCurrentTarget(Player player)
        {
            Transform camera = player.CameraTransform;
            Vector3 origin = camera.position + camera.forward * 0.2f;
            float maxDistance = GetMaxDistance();
            Ray ray = new Ray(origin, camera.forward);

            if (!TrySelectTarget(ray, maxDistance, out GameObject target, out Collider hitCollider))
            {
                Render(player, BuildNoTargetText(camera, maxDistance));
                return;
            }

            Render(player, TruncateForOverlay(SubCommandHelper.BuildObjectInspection(target, hitCollider)));
        }

        private static bool TrySelectTarget(Ray ray, float maxDistance, out GameObject target, out Collider hitCollider)
        {
            target = null;
            hitCollider = null;

            bool hasPhysicsHit = Physics.Raycast(
                ray,
                out RaycastHit physicsHit,
                maxDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            RefreshSceneObjectCache();
            Renderer nearestRenderer = null;
            float nearestRendererDistance = float.PositiveInfinity;

            foreach (Renderer renderer in _cachedRenderers)
            {
                if (renderer == null ||
                    !renderer.enabled ||
                    renderer.gameObject == null ||
                    !renderer.gameObject.activeInHierarchy ||
                    !renderer.gameObject.scene.IsValid())
                    continue;

                Bounds bounds = renderer.bounds;
                if (bounds.Contains(ray.origin) ||
                    !bounds.IntersectRay(ray, out float distance) ||
                    distance < 0f ||
                    distance > maxDistance ||
                    distance >= nearestRendererDistance)
                    continue;

                nearestRenderer = renderer;
                nearestRendererDistance = distance;
            }

            if (nearestRenderer != null && (!hasPhysicsHit || nearestRendererDistance < physicsHit.distance))
            {
                target = nearestRenderer.gameObject;
                return true;
            }

            if (hasPhysicsHit)
            {
                target = physicsHit.collider.gameObject;
                hitCollider = physicsHit.collider;
                return true;
            }

            Transform nearestTransform = FindNearestTransformToRay(ray, maxDistance);
            if (nearestTransform == null)
                return false;

            target = nearestTransform.gameObject;
            return true;
        }

        private static Transform FindNearestTransformToRay(Ray ray, float maxDistance)
        {
            float radius = Math.Max(0.01f, Main.Instance?.Config.PointerInspectorTransformSelectionRadius ?? 0.35f);
            float radiusSquared = radius * radius;
            Transform nearest = null;
            float nearestAlongRay = float.PositiveInfinity;

            foreach (Transform transform in _cachedTransforms)
            {
                if (transform == null ||
                    transform.gameObject == null ||
                    !transform.gameObject.activeInHierarchy ||
                    !transform.gameObject.scene.IsValid())
                    continue;

                Vector3 toTransform = transform.position - ray.origin;
                float alongRay = Vector3.Dot(toTransform, ray.direction);
                if (alongRay < 0f || alongRay > maxDistance || alongRay >= nearestAlongRay)
                    continue;

                Vector3 closestPoint = ray.origin + ray.direction * alongRay;
                if ((transform.position - closestPoint).sqrMagnitude > radiusSquared)
                    continue;

                nearest = transform;
                nearestAlongRay = alongRay;
            }

            return nearest;
        }

        private static void RefreshSceneObjectCache()
        {
            if (DateTime.UtcNow < _sceneCacheExpiresAt)
                return;

            _cachedRenderers = Resources.FindObjectsOfTypeAll<Renderer>()
                .Where(renderer => renderer != null &&
                                   renderer.gameObject != null &&
                                   renderer.gameObject.scene.IsValid())
                .ToArray();
            _cachedTransforms = Resources.FindObjectsOfTypeAll<Transform>()
                .Where(transform => transform != null &&
                                    transform.gameObject != null &&
                                    transform.gameObject.scene.IsValid())
                .ToArray();

            float lifetime = Math.Max(0.5f, Main.Instance?.Config.PointerInspectorSceneCacheLifetime ?? 5f);
            _sceneCacheExpiresAt = DateTime.UtcNow.AddSeconds(lifetime);
        }

        private static string BuildNoTargetText(Transform camera, float maxDistance)
        {
            StringBuilder sb = new();
            sb.AppendLine("<size=20><b><color=#55aaff>GPDebugger · POINTER TRANSFORM</color></b></size>");
            sb.AppendLine($"<size={GetFontSize()}><color=#aaaaaa>No Collider, Renderer, or nearby Transform under crosshair.</color>");
            Append(sb, "Max Distance", maxDistance.ToString("0.###", CultureInfo.InvariantCulture));
            Append(sb, "Camera Position", FormatVector3(camera.position));
            Append(sb, "Camera Forward", FormatVector3(camera.forward));
            sb.Append("</size>");
            return sb.ToString();
        }

        private static string BuildErrorText(Exception exception)
        {
            return $"<size=20><b><color=#ff5555>GPDebugger · POINTER ERROR</color></b></size>\n" +
                   $"<size={GetFontSize()}>{Escape(exception.GetType().Name)}: {Escape(exception.Message)}</size>";
        }

        private static void Render(Player player, string text)
        {
            RemoveHint(player);
            HsmHint hint = new()
            {
                Id = HintId,
                Text = text,
                XCoordinate = Main.Instance?.Config.PointerInspectorXCoordinate ?? 0f,
                YCoordinate = Main.Instance?.Config.PointerInspectorYCoordinate ?? 400f,
                Alignment = HintAlignment.Left,
            };

            player.AddHint(hint);
            DisplayedHints[player.UserId] = hint;
            player.GetPlayerDisplay().ForceUpdate(true);
        }

        private static void RemoveHint(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.UserId))
                return;

            if (DisplayedHints.TryGetValue(player.UserId, out HsmHint hint))
            {
                player.RemoveHint(hint);
                DisplayedHints.Remove(player.UserId);
            }

            player.GetPlayerDisplay().RemoveHint(HintId);
        }

        private static void StopCoroutine(string userId)
        {
            if (!Coroutines.TryGetValue(userId, out CoroutineHandle coroutine))
                return;

            if (coroutine.IsRunning)
                Timing.KillCoroutines(coroutine);
            Coroutines.Remove(userId);
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"({value.x.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                   $"{value.y.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                   $"{value.z.ToString("0.###", CultureInfo.InvariantCulture)})";
        }

        private static void Append(StringBuilder sb, string label, object value)
        {
            sb.Append("<color=#aaaaaa>");
            sb.Append(Escape(label));
            sb.Append(":</color> ");
            sb.AppendLine(Escape(value?.ToString() ?? "null"));
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static float GetUpdateInterval()
            => Math.Max(0.05f, Main.Instance?.Config.PointerInspectorUpdateInterval ?? 0.25f);

        private static float GetMaxDistance()
            => Math.Max(1f, Main.Instance?.Config.PointerInspectorMaxDistance ?? 200f);

        private static int GetFontSize()
        {
            int configured = Main.Instance?.Config.PointerInspectorFontSize ?? 15;
            return Math.Max(8, Math.Min(30, configured));
        }

        private static string TruncateForOverlay(string text)
        {
            string normalized = (text ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .TrimStart('\n');

            string[] lines = normalized.Split(new[] { '\n' }, StringSplitOptions.None);
            int maxLines = Math.Max(5, Main.Instance?.Config.PointerInspectorMaxLines ?? 40);
            if (lines.Length <= maxLines)
                return normalized;

            int contentLineCount = maxLines - 1;
            int omittedLineCount = lines.Length - contentLineCount;
            IEnumerable<string> visibleLines = lines.Take(contentLineCount);
            string notice =
                $"<size={GetFontSize()}><color=#ffb74d>... {omittedLineCount} more lines omitted — use gpdebug print hit for full output.</color></size>";
            return string.Join("\n", visibleLines.Concat(new[] { notice }));
        }

        private static void OnLeft(LeftEventArgs ev)
        {
            if (ev.Player == null || string.IsNullOrWhiteSpace(ev.Player.UserId))
                return;

            Stop(ev.Player);
        }

    }
}
