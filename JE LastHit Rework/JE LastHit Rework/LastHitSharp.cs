namespace JE_LastHit_Rework
{
    using System;

    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Extensions;
    using Ensage.Common.Menu;

    using SharpDX;
    using System.Collections.Generic;
    using System.Linq;

    internal class LastHitSharp
    {
        #region Declare Static Fields

        private static readonly Menu Menu = new Menu("JE LastHit", "jelasthit", true);

        private static Unit creepTarget;
        private static Hero target;
        private static Hero me;

        private static float lastRange;

        private static bool isloaded;

        #endregion

        public static void Init()
        {

            Menu.AddItem(new MenuItem("combatkey", "Combat").SetValue(new KeyBind(32, KeyBindType.Press)));
            Menu.AddItem(new MenuItem("harass", "Last Hit Only").SetValue(new KeyBind('C', KeyBindType.Press)));
            Menu.AddItem(new MenuItem("farmKey", "Farm Key").SetValue(new KeyBind('V', KeyBindType.Press)));
            Menu.AddItem(new MenuItem("kitekey", "Kite key").SetValue(new KeyBind('H', KeyBindType.Press)));
            Menu.AddItem(
                new MenuItem("bonuswindup", "Bonus WindUp time on kitting").SetValue(new Slider(500, 100, 2000))
                    .SetTooltip("Time between attacks in kitting mode"));
            Menu.AddItem(
                new MenuItem("outrange", "Bonus range").SetValue(new Slider(70, 0, 200))
                    .SetTooltip("testing"));
            Menu.AddItem(
                new MenuItem("harassheroes", "Is harass Hero ?").SetValue(new Slider(1, 0, 1))
                    .SetTooltip("testing"));
            Menu.AddToMainMenu();
            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.Load();
        }


        #region OnGameUpdate

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!isloaded)
            {

                me = ObjectMgr.LocalHero;
                if (!Game.IsInGame || me == null)
                {
                    return;
                }
                isloaded = true;
                target = null;
                Game.PrintMessage(
                    "<font face='Tahoma'><font color='#000000'>[--</font> <font color='#000000'>JE LastHit</font> by <font color='#000000'>JE</font> loaded! <font color='#000000'>--]</font></font>",
                    MessageType.LogMessage);
            }

            if (me == null || !me.IsValid)
            {
                isloaded = false;
                me = ObjectMgr.LocalHero;

                target = null;

                return;
            }

            if (Game.IsPaused)
            {
                return;
            }


            if (me.IsAlive)
            {
                lastRange = me.GetAttackRange() + me.HullRadius + Menu.Item("outrange").GetValue<Slider>().Value;
            }


            if (target != null && (!target.IsValid || !target.IsVisible || !target.IsAlive || target.Health <= 0))
            {
                target = null;
            }
            var canCancel = Orbwalking.CanCancelAnimation();
            if (canCancel)
            {
                if (target != null && !target.IsVisible && !Orbwalking.AttackOnCooldown(target))
                {
                    target = me.ClosestToMouseTarget(128);
                }
                else if (target == null || !Orbwalking.AttackOnCooldown(target))
                {
                    var bestAa = me.BestAATarget();
                    if (bestAa != null)
                    {
                        target = me.BestAATarget();
                    }
                }
                if (Game.IsKeyDown(Menu.Item("farmKey").GetValue<KeyBind>().Key)
                    && (creepTarget == null || !creepTarget.IsValid || !creepTarget.IsVisible || !creepTarget.IsAlive
                        || creepTarget.Health <= 0 || !Orbwalking.AttackOnCooldown(creepTarget)))
                {
                    
                    creepTarget = GetLowestHPCreep(me,null);
                    //creepTarget = GetAllLowestHPCreep(me);
                    creepTarget = KillableCreep(true, creepTarget);
                }

                if (Game.IsKeyDown(Menu.Item("harass").GetValue<KeyBind>().Key)
                    && (creepTarget == null || !creepTarget.IsValid || !creepTarget.IsVisible /*|| !creepTarget.IsAlive*/
                        || creepTarget.Health <= 0 || !Orbwalking.AttackOnCooldown(creepTarget)))
                {
                    creepTarget = GetLowestHPCreep(me,null);
                    //creepTarget = GetAllLowestHPCreep(me);
                    creepTarget = KillableCreep(false, creepTarget);

                 
                }
            }

            if (Game.IsChatOpen)
            {
                return;
            }
            if (Game.IsKeyDown(Menu.Item("harass").GetValue<KeyBind>().Key))
            {
                if (creepTarget == null)
                {
                    //Console.WriteLine("null ");
                    if (target != null && !target.IsVisible)
                    {
                        var closestToMouse = me.ClosestToMouseTarget(128);
                        if (closestToMouse != null)
                        {
                            target = closestToMouse;
                        }
                    }
                    else if (Menu.Item("harassheroes").GetValue<Slider>().Value == 1)
                    {
                        target = me.BestAATarget();
                    }
                    Orbwalking.Orbwalk(target, 500);
                }
                else
                {
                    //Console.WriteLine("found creep ");
                    Orbwalking.Orbwalk(creepTarget, 500);
                }
            }
            if (Game.IsKeyDown(Menu.Item("farmKey").GetValue<KeyBind>().Key))
            {
                Orbwalking.Orbwalk(creepTarget);
            }
            if (Game.IsKeyDown(Menu.Item("combatkey").GetValue<KeyBind>().Key))
            {
                Orbwalking.Orbwalk(target, attackmodifiers: true);
            }
            if (Game.IsKeyDown(Menu.Item("kitekey").GetValue<KeyBind>().Key))
            {
                Orbwalking.Orbwalk(
                    target,
                    attackmodifiers: true,
                    bonusWindupMs: Menu.Item("bonusWindup").GetValue<Slider>().Value);
            }
        }

        #endregion



        #region predict

        public static Unit GetAllLowestHPCreep(Hero source)
        {
            try
            {
                var attackRange = source.GetAttackRange();
                var lowestHp =
                    ObjectMgr.GetEntities<Unit>()
                        .Where(
                            x =>
                            (x.ClassID == ClassID.CDOTA_BaseNPC_Tower || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Creep
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Additive
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Barracks
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Building
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Creature) && x.IsAlive && x.IsVisible
                            /*&& x.Team != source.Team*/ && x.Distance2D(source) < attackRange + 100)
                        .OrderBy(creep => creep.Health)
                        .DefaultIfEmpty(null)
                        .FirstOrDefault();
                return lowestHp;
            }
            catch (Exception)
            {
                //no   
            }
            return null;
        }
        public static Unit GetLowestHPCreep(Hero source,Unit markedcreep)
        {
            try
            {
                var attackRange = source.GetAttackRange();
                var lowestHp =
                    ObjectMgr.GetEntities<Unit>()
                        .Where(
                            x =>
                            (x.ClassID == ClassID.CDOTA_BaseNPC_Tower || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Creep
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Additive
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Barracks
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Building
                             || x.ClassID == ClassID.CDOTA_BaseNPC_Creature) && x.IsAlive && x.IsVisible
                            && x.Team != source.Team && x.Distance2D(source) < attackRange + 100 && x != markedcreep)
                        .OrderBy(creep => creep.Health)
                        .DefaultIfEmpty(null)
                        .FirstOrDefault();
                return lowestHp;
            }
            catch (Exception)
            {
                //no   
            }
            return null;
        }
        private static Unit KillableCreep(bool islaneclear, Unit minion)
        {
            //var minion = ObjectMgr.GetEntities<Creep>()
            //       .Where(creep => creep.IsAlive && me.Distance2D(creep) <= me.GetAttackRange())
            //       .OrderBy(creep => creep.Health).DefaultIfEmpty(null).FirstOrDefault();

            double test = 0;
            if (minion != null)
            {
                

                var missilespeed = GetProjectileSpeed(me);
                var time = me.IsRanged == false ? 0 : UnitDatabase.GetAttackBackswing(me) + (me.Distance2D(minion) / missilespeed);
                test = time * minion.AttacksPerSecond * minion.MinimumDamage;


                // Console.WriteLine("test " + test + " time " + time + " distance " + me.Distance2D(minion) / missilespeed);
                if (minion != null && (minion.Health) < GetPhysDamageOnUnit(minion, test))
                {

                    if (me.CanAttack())
                    {
                        return minion;
                    }
                }
                Unit minion2 = GetAllLowestHPCreep(me);
                test = time * minion2.AttacksPerSecond * minion2.MinimumDamage;
                if (minion2 != null && (minion2.Health) < GetPhysDamageOnUnit(minion2, test))
                {

                    if (me.CanAttack())
                    {
                        return minion2;
                    }
                }
                return null;
                if (minion != null && (minion.Health) >= GetPhysDamageOnUnit(minion, test) && (minion.Health) <= (GetPhysDamageOnUnit(minion, test) + me.MinimumDamage*2))
                {
                    if ((minion.Health) >= (GetPhysDamageOnUnit(minion, test) + me.MinimumDamage*1.5))
                    {
                        return null;
                    }
                    else
                    {
                        //return GetLowestHPCreep(me, minion);
                    }
                    //if (me.CanAttack())
                    //{
                    //    return null;
                    //}
                }
            }
            return islaneclear == true ? minion : null;
        }

        private static double GetPhysDamageOnUnit(Unit unit, double bonusdamage)
        {
            Item quelling_blade = me.FindItem("item_quelling_blade");
            double PhysDamage = me.MinimumDamage + me.BonusDamage;
            if (quelling_blade != null)
            {
                if (me.IsRanged)
                {
                    PhysDamage = me.MinimumDamage * 1.15 + me.BonusDamage;
                }
                else
                {
                    PhysDamage = me.MinimumDamage * 1.4 + me.BonusDamage;

                }
            }
            double _damageMP = 1 - 0.06 * unit.Armor / (1 + 0.06 * Math.Abs(unit.Armor));

            double realDamage = (bonusdamage + PhysDamage) * _damageMP;

            return realDamage;
        }


        public static float GetProjectileSpeed(Hero unit)
        {
            //Console.WriteLine(unit.AttacksPerSecond * Game.FindKeyValues(unit.Name + "/AttackRate", KeyValueSource.Hero).FloatValue / 0.01);
            //var ProjectileSpeed = Game.FindKeyValues(unit.Name + "/ProjectileSpeed", KeyValueSource.Unit).FloatValue;
            var ProjectileSpeed = UnitDatabase.GetByName(unit.Name).ProjectileSpeed;

            return (float)ProjectileSpeed;
        }
        #endregion
    }

}