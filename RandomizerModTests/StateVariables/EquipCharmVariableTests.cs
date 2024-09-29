﻿using RandomizerCore.Logic;
using RandomizerMod.RC;
using RandomizerMod.RC.StateVariables;
using FluentAssertions;
using RandomizerCore.Logic.StateLogic;
using EquipResult = RandomizerMod.RC.StateVariables.EquipCharmVariable.EquipResult;

namespace RandomizerModTests.StateVariables
{
    [Collection("Logic Collection")]
    public class EquipCharmVariableTests
    {
        LogicFixture Fix { get; }

        public EquipCharmVariableTests(LogicFixture fix)
        {
            Fix = fix;
        }

        public static Dictionary<string, int> CharmStateBase => new() { ["NOPASSEDCHARMEQUIP"] = 0 };

        [Fact]
        public void BasicEquipRequirementsTest()
        {
            LogicManager lm = Fix.LM;
            Term swarmTerm = lm.GetTermStrict("Gathering_Swarm");
            EquipCharmVariable swarm = (EquipCharmVariable)lm.GetVariableStrict(EquipCharmVariable.GetName(swarmTerm.Name));
            var pm = Fix.GetProgressionManager(new() { [swarmTerm.Name] = 1, ["NOTCHES"] = 1 });

            LazyStateBuilder state = new(lm.StateManager.DefaultState);
            swarm.TryEquip(null, pm, ref state).Should().BeFalse();
            swarm.IsEquipped(state).Should().BeFalse();
            state.GetInt(lm.StateManager.GetIntStrict("USEDNOTCHES")).Should().Be(0);
            state.GetInt(lm.StateManager.GetIntStrict("MAXNOTCHCOST")).Should().Be(0);

            state.SetBool(lm.StateManager.GetBoolStrict("NOPASSEDCHARMEQUIP"), false);
            swarm.TryEquip(null, pm, in state, out LazyStateBuilder result).Should().BeTrue();
            swarm.IsEquipped(state).Should().BeFalse();
            state.GetInt(lm.StateManager.GetIntStrict("USEDNOTCHES")).Should().Be(0);
            state.GetInt(lm.StateManager.GetIntStrict("MAXNOTCHCOST")).Should().Be(0);
            swarm.IsEquipped(result).Should().BeTrue();
            result.GetInt(lm.StateManager.GetIntStrict("USEDNOTCHES")).Should().Be(1);
            result.GetInt(lm.StateManager.GetIntStrict("MAXNOTCHCOST")).Should().Be(1);

            result = new(state);
            swarm.SetUnequippable(ref state);
            swarm.TryEquip(null, pm, ref state).Should().BeFalse();
            swarm.IsEquipped(state).Should().BeFalse();
            state.GetInt(lm.StateManager.GetIntStrict("USEDNOTCHES")).Should().Be(0);
            state.GetInt(lm.StateManager.GetIntStrict("MAXNOTCHCOST")).Should().Be(0);

            swarm.TryEquip(null, pm, ref result).Should().BeTrue();
            swarm.IsEquipped(result).Should().BeTrue();
            result.GetInt(lm.StateManager.GetIntStrict("USEDNOTCHES")).Should().Be(1);
            result.GetInt(lm.StateManager.GetIntStrict("MAXNOTCHCOST")).Should().Be(1);

            swarm.TryEquip(null, pm, ref result).Should().BeTrue();
            swarm.IsEquipped(result).Should().BeTrue();
            result.GetInt(lm.StateManager.GetIntStrict("USEDNOTCHES")).Should().Be(1);
            result.GetInt(lm.StateManager.GetIntStrict("MAXNOTCHCOST")).Should().Be(1);
        }

        [Theory]
        [InlineData(1, (int[])[1], (bool[])[true], false)]
        [InlineData(3, (int[])[1, 2, 1], (bool[])[true, true, true], true)]
        [InlineData(3, (int[])[1, 2, 2], (bool[])[true, true, false], false)]
        [InlineData(3, (int[])[2, 2, 1], (bool[])[true, true, false], true)]
        [InlineData(3, (int[])[2, 2, 0], (bool[])[true, true, true], true)]
        [InlineData(3, (int[])[4, 1, 1], (bool[])[true, true, true], true)]
        [InlineData(3, (int[])[4, 1, 1, 1], (bool[])[true, true, true, false], true)]
        [InlineData(3, (int[])[1, 1, 1, 4], (bool[])[true, true, true, false], false)]
        [InlineData(3, (int[])[0, 1, 0], (bool[])[true, true, true], false)]
        public void EquipCharmNotchTests(int notches, int[] notchCosts, bool[] equipResults, bool endedOvercharmed)
        {
            LogicManager lm = Fix.LM;

            var terms = lm.Terms.GetTermList(TermType.SignedByte).Skip(lm.GetTermStrict("Gathering_Swarm").Index).Take(notchCosts.Length);
            var charms = terms.Select(t => lm.GetVariableStrict(EquipCharmVariable.GetName(t.Name))).Cast<EquipCharmVariable>().ToArray();

            var state = Fix.GetState(CharmStateBase);
            var pm = Fix.GetProgressionManager(terms.ToDictionary(t => t.Name, t => 1));
            RandoModContext ctx = (RandoModContext)pm.ctx;
            for (int i = 0; i < notchCosts.Length; i++) ctx.notchCosts[i] = notchCosts[i];
            pm.Set("NOTCHES", notches);
            
            for (int i = 0; i < notchCosts.Length; i++)
            {
                charms[i].TryEquip(null, pm, ref state).Should().Be(equipResults[i], lm.StateManager.PrettyPrint(state));
            }

            state.GetBool(lm.StateManager.GetBoolStrict("OVERCHARMED")).Should().Be(endedOvercharmed, lm.StateManager.PrettyPrint(state));

        }


    }
}
