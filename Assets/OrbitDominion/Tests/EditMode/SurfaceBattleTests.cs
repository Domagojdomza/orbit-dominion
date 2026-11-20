using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OrbitDominion.Tests
{
    public sealed class SurfaceBattleTests
    {
        [Test]
        public void RifleCadenceReloadAndInputLocksDoNotSpendRounds()
        {
            var state = new SurfaceBattleState();
            Assert.IsFalse(state.Fire(true, true, false, false));
            Assert.IsFalse(state.Fire(false, false, false, false));
            Assert.IsFalse(state.Fire(true, false, true, false));
            Assert.IsFalse(state.Fire(true, false, false, true));
            Assert.AreEqual(18, state.Rounds);
            for (int i = 0; i < 18; i++)
            {
                Assert.IsTrue(state.Fire(true, false, false, false));
                Assert.IsFalse(state.Fire(true, false, false, false));
                state.Tick(.2f, false);
            }
            Assert.AreEqual(0, state.Rounds);
            Assert.IsFalse(state.Fire(true, false, false, false));
            Assert.Greater(state.ReloadRemaining, 0);
            state.Tick(10, true); Assert.AreEqual(0, state.Rounds);
            state.Tick(2, false); Assert.AreEqual(18, state.Rounds);
        }

        [Test]
        public void BattleSavePreservesWoundsAndDefeatedGarrisonRejectsCorruption()
        {
            var worlds = new List<WorldDefinition> { new("cinder-01", "PYRE", Vector3.zero, 310, Color.red, Color.gray, Color.gray, Color.red, .5f, .5f) };
            var state = new SurfaceBattleState();
            state.Suit.Damage(85, CombatFaction.Hostile, CombatFaction.Union);
            state.Guard("cinder-01", 0).Damage(100, CombatFaction.Union, CombatFaction.Hostile);
            Assert.IsFalse(state.Suit.Damage(20, CombatFaction.Union, CombatFaction.Union));
            state.Fire(true, false, false, false); state.Reload(true);
            var save = JsonUtility.FromJson<SurfaceBattleSave>(JsonUtility.ToJson(state.Snapshot()));
            Assert.IsTrue(SurfaceBattleState.ValidSave(save, worlds));
            var restored = new SurfaceBattleState(); restored.Restore(save);
            Assert.AreEqual(75, restored.Suit.Hull); Assert.IsFalse(restored.Guard("cinder-01", 0).Alive);
            Assert.AreEqual(17, restored.Rounds); Assert.Greater(restored.ReloadRemaining, 0);
            save.rounds = -1; Assert.IsFalse(SurfaceBattleState.ValidSave(save, worlds));
            save.rounds = 17; save.guards.Add(save.guards[0]); Assert.IsFalse(SurfaceBattleState.ValidSave(save, worlds));
            Assert.IsTrue(SurfaceBattleState.ValidSave(null, worlds));
        }
    }
}
