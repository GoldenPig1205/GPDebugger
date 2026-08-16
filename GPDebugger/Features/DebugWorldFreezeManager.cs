using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Events.Arguments.PlayerEvents;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LabPlayerEvents = LabApi.Events.Handlers.PlayerEvents;

namespace GPDebugger.Features
{
    internal static class DebugWorldFreezeManager
    {
        private const float RefreshInterval = 0.25f;

        private static readonly HashSet<Player> EnsnaredPlayers = new();
        private static readonly Dictionary<Rigidbody, RigidbodyState> RigidbodyStates = new();
        private static readonly Dictionary<Pickup, bool> PickupStates = new();
        private static readonly Dictionary<TimeGrenade, GrenadeFuseState> GrenadeFuseStates = new();

        private static CoroutineHandle _refreshCoroutine;
        private static string _excludedUserId;
        private static Transform _excludedTransform;
        private static bool _isRegistered;

        internal static bool IsActive { get; private set; }
        internal static string ExcludedNickname { get; private set; }

        internal static void Register()
        {
            if (_isRegistered)
                return;

            LabPlayerEvents.ThrewProjectile += OnThrewProjectile;
            _isRegistered = true;
        }

        internal static void Unregister()
        {
            if (!_isRegistered)
                return;

            LabPlayerEvents.ThrewProjectile -= OnThrewProjectile;
            _isRegistered = false;
        }

        internal static bool Freeze(Player excludedPlayer, out string response)
        {
            if (excludedPlayer == null || !excludedPlayer.IsConnected)
            {
                response = "Only an in-game player can start world freeze mode.";
                return false;
            }

            if (IsActive)
            {
                response = $"World freeze is already active. Excluded player: {ExcludedNickname}.";
                return false;
            }

            DebugTimeManager.Restore();
            _excludedUserId = excludedPlayer.UserId;
            _excludedTransform = excludedPlayer.Transform;
            ExcludedNickname = excludedPlayer.Nickname;
            IsActive = true;

            ApplyFreeze();
            _refreshCoroutine = Timing.RunCoroutine(RefreshLoop());

            response =
                $"World FREEZE enabled. {ExcludedNickname} is excluded and can move.\n" +
                $"Frozen: {EnsnaredPlayers.Count} player(s), {RigidbodyStates.Count} rigidbody(s), " +
                $"{PickupStates.Count} pickup(s), {GrenadeFuseStates.Count} grenade fuse(s).\n" +
                "Other game timers and scripted animations may continue. Use 'gpdebug time unfreeze' or 'gpdebug time resume' to restore.";
            return true;
        }

        internal static int Resume()
        {
            if (!IsActive)
                return 0;

            IsActive = false;
            if (_refreshCoroutine.IsRunning)
                Timing.KillCoroutines(_refreshCoroutine);

            int restored = 0;

            foreach (Player player in EnsnaredPlayers.ToArray())
            {
                if (player == null || !player.IsConnected)
                    continue;

                try
                {
                    player.DisableEffect(EffectType.Ensnared);
                    restored++;
                }
                catch (Exception exception)
                {
                    Log.Debug($"Could not unfreeze player {player.Nickname}: {exception.Message}");
                }
            }

            foreach (KeyValuePair<Rigidbody, RigidbodyState> entry in RigidbodyStates.ToArray())
            {
                Rigidbody body = entry.Key;
                if (body == null)
                    continue;

                try
                {
                    RigidbodyState state = entry.Value;
                    body.constraints = state.Constraints;

                    if (!state.WasKinematic && !body.isKinematic)
                    {
                        body.linearVelocity = state.Velocity;
                        body.angularVelocity = state.AngularVelocity;
                        if (state.WasSleeping)
                            body.Sleep();
                        else
                            body.WakeUp();

                        ForcePickupPhysicsSync(body);
                    }

                    restored++;
                }
                catch (Exception exception)
                {
                    Log.Debug($"Could not restore Rigidbody: {exception.Message}");
                }
            }

            foreach (KeyValuePair<Pickup, bool> entry in PickupStates.ToArray())
            {
                Pickup pickup = entry.Key;
                if (pickup == null || pickup.GameObject == null)
                    continue;

                try
                {
                    pickup.IsLocked = entry.Value;
                    restored++;
                }
                catch (Exception exception)
                {
                    Log.Debug($"Could not restore pickup: {exception.Message}");
                }
            }

            foreach (KeyValuePair<TimeGrenade, GrenadeFuseState> entry in GrenadeFuseStates.ToArray())
            {
                TimeGrenade grenade = entry.Key;
                if (grenade == null || grenade.gameObject == null)
                    continue;

                try
                {
                    grenade.TargetTime = NetworkTime.time + Math.Max(0.05d, entry.Value.RemainingFuseTime);
                    restored++;
                }
                catch (Exception exception)
                {
                    Log.Debug($"Could not restore grenade fuse: {exception.Message}");
                }
            }

            EnsnaredPlayers.Clear();
            RigidbodyStates.Clear();
            PickupStates.Clear();
            GrenadeFuseStates.Clear();
            _excludedUserId = null;
            _excludedTransform = null;
            ExcludedNickname = null;
            _refreshCoroutine = default;
            return restored;
        }

        private static IEnumerator<float> RefreshLoop()
        {
            while (IsActive)
            {
                yield return Timing.WaitForSeconds(RefreshInterval);

                try
                {
                    ApplyFreeze();
                }
                catch (Exception exception)
                {
                    Log.Error($"World freeze refresh failed: {exception}");
                }
            }
        }

        private static void ApplyFreeze()
        {
            FreezePlayers();
            PauseGrenadeFuses();
            FreezeRigidbodies();
            LockPickups();
        }

        private static void FreezePlayers()
        {
            foreach (Player player in Player.List.ToArray())
            {
                if (player == null || !player.IsConnected || player.IsHost ||
                    string.Equals(player.UserId, _excludedUserId, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    bool isEnsnared = player.TryGetEffect(EffectType.Ensnared, out var effect) && effect.IsEnabled;
                    if (EnsnaredPlayers.Contains(player))
                    {
                        if (!isEnsnared)
                            player.EnableEffect(EffectType.Ensnared);

                        continue;
                    }

                    if (!isEnsnared)
                    {
                        player.EnableEffect(EffectType.Ensnared);
                        EnsnaredPlayers.Add(player);
                    }
                }
                catch (Exception exception)
                {
                    Log.Debug($"Could not freeze player {player.Nickname}: {exception.Message}");
                }
            }
        }

        private static void FreezeRigidbodies()
        {
            foreach (Rigidbody body in Resources.FindObjectsOfTypeAll<Rigidbody>())
            {
                if (body == null || body.gameObject == null || !body.gameObject.scene.IsValid() || IsExcluded(body.transform))
                    continue;

                try
                {
                    CaptureAndFreezeRigidbody(body, overwriteMotion: false);
                }
                catch (Exception exception)
                {
                    Log.Debug($"Could not freeze Rigidbody {body.name}: {exception.Message}");
                }
            }
        }

        private static void OnThrewProjectile(PlayerThrewProjectileEventArgs ev)
        {
            if (!IsActive || ev?.Projectile?.Base == null)
                return;

            try
            {
                ThrownProjectile projectile = ev.Projectile.Base;
                if (projectile is TimeGrenade grenade)
                    PauseGrenadeFuse(grenade);

                Rigidbody body = projectile.GetComponent<Rigidbody>();
                if (body != null)
                {
                    Vector3 actualVelocity = body.linearVelocity;
                    Vector3 throwForward = projectile.transform.forward;
                    Vector3 throwUp = projectile.transform.up;
                    float verticalFactor = 1f - Mathf.Abs(Vector3.Dot(throwForward, Vector3.up));
                    Vector3 inheritedVelocity = ev.Player?.Velocity ?? Vector3.zero;
                    Vector3 reconstructedVelocity =
                        (throwForward + throwUp * ev.ProjectileSettings.UpwardsFactor * verticalFactor) *
                        ev.ProjectileSettings.StartVelocity + inheritedVelocity;

                    CaptureAndFreezeRigidbody(body, overwriteMotion: true);
                    if (RigidbodyStates.TryGetValue(body, out RigidbodyState state))
                    {
                        Vector3 velocityToRestore = reconstructedVelocity.sqrMagnitude > 0.01f
                            ? reconstructedVelocity
                            : actualVelocity;
                        state.UpdateMotion(velocityToRestore, ev.ProjectileSettings.StartTorque, wasSleeping: false);
                        Log.Debug(
                            $"Captured thrown projectile {projectile.name}: actual={FormatVector(actualVelocity)}, " +
                            $"reconstructed={FormatVector(reconstructedVelocity)}, stored={FormatVector(velocityToRestore)}");
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error($"Could not capture thrown projectile during world freeze: {exception}");
            }
        }

        private static void CaptureAndFreezeRigidbody(Rigidbody body, bool overwriteMotion)
        {
            if (!RigidbodyStates.TryGetValue(body, out RigidbodyState state))
            {
                state = new RigidbodyState(
                    body.isKinematic,
                    body.constraints,
                    body.linearVelocity,
                    body.angularVelocity,
                    body.IsSleeping());
                RigidbodyStates[body] = state;
            }
            else if (overwriteMotion)
            {
                state.UpdateMotion(body.linearVelocity, body.angularVelocity, body.IsSleeping());
            }

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        private static void ForcePickupPhysicsSync(Rigidbody body)
        {
            ItemPickupBase pickup = body.GetComponentInParent<ItemPickupBase>();
            if (pickup?.PhysicsModule is not PickupStandardPhysics physics)
                return;

            physics._serverEverDecelerated = false;
            physics._serverPrevSleeping = false;
            physics._serverNextUpdateTime = 0d;
            physics.ServerSetSyncData(physics._serverWriteRigidbody);
            physics.ServerSendRpc(physics._serverWriteRigidbody);
        }

        private static void LockPickups()
        {
            foreach (Pickup pickup in Pickup.List.ToArray())
            {
                if (pickup == null || pickup.GameObject == null)
                    continue;

                try
                {
                    if (!PickupStates.ContainsKey(pickup))
                        PickupStates[pickup] = pickup.IsLocked;

                    pickup.IsLocked = true;
                }
                catch (Exception exception)
                {
                    Log.Debug($"Could not lock pickup: {exception.Message}");
                }
            }
        }

        private static void PauseGrenadeFuses()
        {
            foreach (TimeGrenade grenade in Resources.FindObjectsOfTypeAll<TimeGrenade>())
            {
                if (grenade == null || grenade.gameObject == null || !grenade.gameObject.scene.IsValid() || grenade._alreadyDetonated)
                    continue;

                try
                {
                    PauseGrenadeFuse(grenade);
                }
                catch (Exception exception)
                {
                    Log.Debug($"Could not pause grenade fuse {grenade.name}: {exception.Message}");
                }
            }
        }

        private static void PauseGrenadeFuse(TimeGrenade grenade)
        {
            if (!GrenadeFuseStates.ContainsKey(grenade))
            {
                if (grenade.TargetTime <= 0d)
                    return;

                double remaining = Math.Max(0.05d, grenade.TargetTime - NetworkTime.time);
                GrenadeFuseStates[grenade] = new GrenadeFuseState(remaining);
            }

            grenade.TargetTime = 0d;
        }

        private static bool IsExcluded(Transform target)
            => _excludedTransform != null && (target == _excludedTransform || target.IsChildOf(_excludedTransform));

        private static string FormatVector(Vector3 value)
            => $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";

        private sealed class RigidbodyState
        {
            internal RigidbodyState(
                bool isKinematic,
                RigidbodyConstraints constraints,
                Vector3 velocity,
                Vector3 angularVelocity,
                bool wasSleeping)
            {
                WasKinematic = isKinematic;
                Constraints = constraints;
                Velocity = velocity;
                AngularVelocity = angularVelocity;
                WasSleeping = wasSleeping;
            }

            internal bool WasKinematic { get; }
            internal RigidbodyConstraints Constraints { get; }
            internal Vector3 Velocity { get; private set; }
            internal Vector3 AngularVelocity { get; private set; }
            internal bool WasSleeping { get; private set; }

            internal void UpdateMotion(Vector3 velocity, Vector3 angularVelocity, bool wasSleeping)
            {
                Velocity = velocity;
                AngularVelocity = angularVelocity;
                WasSleeping = wasSleeping;
            }
        }

        private sealed class GrenadeFuseState
        {
            internal GrenadeFuseState(double remainingFuseTime)
            {
                RemainingFuseTime = remainingFuseTime;
            }

            internal double RemainingFuseTime { get; }
        }
    }
}
