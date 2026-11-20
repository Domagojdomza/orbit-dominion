using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitDominion
{
    [Serializable] public sealed class SurfaceGuardSave
    {
        public string planet;
        public int index;
        public float hull = 80, shield = 20, sinceHit;
    }
    [Serializable] public sealed class SurfaceBattleSave
    {
        public float hull = 100, shield = 60, sinceHit, reload, cooldown;
        public int rounds = SurfaceBattleState.Magazine;
        public List<SurfaceGuardSave> guards = new();
    }

    // Authoritative suit, rifle and garrison state. Scene adapters decide line of sight and input.
    public sealed class SurfaceBattleState
    {
        public const int Magazine = 18, GuardsPerWorld = 3;
        public const float ShotDamage = 34, Range = 95, FireInterval = .19f, ReloadSeconds = 1.65f;
        public readonly VitalState Suit = new(100, 60);
        readonly Dictionary<string, (SurfaceGuardSave record, VitalState vital)> guards = new();
        public int Rounds { get; private set; } = Magazine;
        public float ReloadRemaining { get; private set; }
        public float Cooldown { get; private set; }

        public VitalState Guard(string planet, int index)
        {
            string id = planet + "/" + index;
            if (!guards.TryGetValue(id, out var actor))
            {
                actor = (new SurfaceGuardSave { planet = planet, index = index }, new VitalState(80, 20));
                guards.Add(id, actor);
            }
            return actor.vital;
        }

        public void Tick(float dt, bool paused)
        {
            if (paused || !SectorState.Finite(dt) || dt <= 0) return;
            Suit.Tick(dt, 5, 14); Cooldown = Mathf.Max(0, Cooldown - dt);
            foreach (var actor in guards.Values) actor.vital.Tick(dt, 8, 3);
            if (ReloadRemaining <= 0) return;
            ReloadRemaining = Mathf.Max(0, ReloadRemaining - dt);
            if (ReloadRemaining == 0) Rounds = Magazine;
        }

        public bool Reload(bool permitted)
        {
            if (!permitted || !Suit.Alive || Rounds == Magazine || ReloadRemaining > 0) return false;
            ReloadRemaining = ReloadSeconds; return true;
        }

        public bool Fire(bool onFoot, bool paused, bool building, bool sprinting)
        {
            if (!onFoot || building || sprinting || !CombatRules.CanFire(Suit.Alive, paused, false, Cooldown) || ReloadRemaining > 0) return false;
            if (Rounds == 0) { Reload(true); return false; }
            Rounds--; Cooldown = FireInterval;
            return true;
        }

        public SurfaceBattleSave Snapshot()
        {
            var result = new SurfaceBattleSave { hull = Suit.Hull, shield = Suit.Shield, sinceHit = Suit.SinceHit,
                rounds = Rounds, reload = ReloadRemaining, cooldown = Cooldown };
            foreach (var actor in guards.Values) result.guards.Add(new SurfaceGuardSave {
                planet = actor.record.planet, index = actor.record.index, hull = actor.vital.Hull,
                shield = actor.vital.Shield, sinceHit = actor.vital.SinceHit });
            return result;
        }

        public void Restore(SurfaceBattleSave save)
        {
            guards.Clear(); Suit.Reset(); Rounds = Magazine; ReloadRemaining = Cooldown = 0;
            if (save == null) return;
            Suit.Restore(save.hull, save.shield, save.sinceHit);
            Rounds = save.rounds; ReloadRemaining = save.reload; Cooldown = save.cooldown;
            foreach (var actor in save.guards) Guard(actor.planet, actor.index).Restore(actor.hull, actor.shield, actor.sinceHit);
        }

        public static bool ValidSave(SurfaceBattleSave save, IList<WorldDefinition> worlds)
        {
            if (save == null) return true;
            if (!ValidVitals(save.hull, save.shield, save.sinceHit, 100, 60) || save.rounds < 0 || save.rounds > Magazine ||
                !Within(save.reload, ReloadSeconds) || !Within(save.cooldown, FireInterval) || save.guards == null ||
                save.guards.Count > worlds.Count * GuardsPerWorld) return false;
            var known = new HashSet<string>(); foreach (var world in worlds) known.Add(world.id);
            var ids = new HashSet<string>();
            foreach (var actor in save.guards)
                if (actor == null || !known.Contains(actor.planet) || actor.index < 0 || actor.index >= GuardsPerWorld ||
                    !ids.Add(actor.planet + "/" + actor.index) || !ValidVitals(actor.hull, actor.shield, actor.sinceHit, 80, 20)) return false;
            return true;
        }
        static bool Within(float v, float maximum) => SectorState.Finite(v) && v >= 0 && v <= maximum + .0001f;
        static bool ValidVitals(float hull, float shield, float sinceHit, float maxHull, float maxShield)
            => Within(hull, maxHull) && Within(shield, maxShield) && Within(sinceHit, 10000000);
    }
}
