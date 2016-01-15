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
        private static int tickRate = 100;
        private static float lastRange;
        private static ParticleEffect rangeDisplay;
        private static bool isloaded;
        private static Bool _screenSizeLoaded = false;
        private static float _screenX;
        private static readonly Dictionary<Unit, string> CreepsDictionary = new Dictionary<Unit, string>();
        private static readonly Dictionary<Unit, Team> CreepsTeamDictionary = new Dictionary<Unit, Team>();
        #endregion

        public static void Init()
        {

            Menu.AddItem(new MenuItem("combatkey", "Chase mode").SetValue(new KeyBind(32, KeyBindType.Press)));
            Menu.AddItem(new MenuItem("harass", "Lasthit mode").SetValue(new KeyBind('C', KeyBindType.Press)));
            Menu.AddItem(new MenuItem("farmKey", "Farm mode").SetValue(new KeyBind('V', KeyBindType.Press)));
            Menu.AddItem(new MenuItem("kitekey", "Kite mode").SetValue(new KeyBind('H', KeyBindType.Press)));
            Menu.AddItem(
                new MenuItem("bonuswindup", "Bonus WindUp time on kitting").SetValue(new Slider(500, 100, 2000))
                    .SetTooltip("Time between attacks in kitting mode"));
            Menu.AddItem(
                new MenuItem("hpleftcreep", "Mark hp ?").SetValue(true)
                    .SetTooltip("testing"));
            Menu.AddItem(
                new MenuItem("showatkrange", "Show attack range ?").SetValue(true)
                    .SetTooltip("testing"));
            Menu.AddItem(
                new MenuItem("harassheroes", "Harass in lasthit mode ?").SetValue(true)
                    .SetTooltip("testing"));
            Menu.AddItem(
                new MenuItem("denied", "Deny creep ?").SetValue(true)
                    .SetTooltip("testing"));
            Menu.AddItem(
                new MenuItem("outrange", "Bonus range").SetValue(new Slider(70, 0, 200))
                    .SetTooltip("testing"));
            Menu.AddToMainMenu();


            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.Load();
            
            

            if (rangeDisplay == null)
            {
                return;
            }
            rangeDisplay = null;
        }


        #region OnGameUpdate
         private static void drawhpbar()
         {
             var attackRange = me.GetAttackRange();
             var enemies =
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
                             && x.Team != me.Team && x.Distance2D(me) < attackRange + 500);
             foreach (var enemy in enemies.Where(x => x != null ))
            {

<<<<<<< HEAD

                var health = enemy.Health;
                var maxHealth = enemy.MaximumHealth;
               
                var damge = (float)GetPhysDamageOnUnit(enemy, 0);
                var hpleft = health;
                var hpperc = hpleft / maxHealth;

                var dmgperc = Math.Min(damge, health) / maxHealth;
                Vector2 hbarpos;

=======

                var health = enemy.Health;
                var maxHealth = enemy.MaximumHealth;
               
                var damge = (float)GetPhysDamageOnUnit(enemy, 0);
                var hpleft = health;
                var hpperc = hpleft / maxHealth;

                var dmgperc = Math.Min(damge, health) / maxHealth;
                Vector2 hbarpos;

>>>>>>> origin/master
                hbarpos = HUDInfo.GetHPbarPosition(enemy);
                
                Vector2 screenPos;
                var enemyPos = enemy.Position + new Vector3(0, 0, enemy.HealthBarOffset);
                if (!Drawing.WorldToScreen(enemyPos, out screenPos)) continue;

                var start = screenPos;


                hbarpos.X = start.X - (HUDInfo.GetHPBarSizeX(enemy) / 2);
                hbarpos.Y = start.Y;
                var hpvarx = hbarpos.X;
                var hpbary = hbarpos.Y;
                float a = (float)Math.Round((damge * HUDInfo.GetHPBarSizeX(enemy)) / (enemy.MaximumHealth));
                var position = hbarpos + new Vector2(hpvarx * hpperc +10, -10);
<<<<<<< HEAD
                //Console.WriteLine("here " + damge.ToString());
=======
                Console.WriteLine("here " + damge.ToString());
>>>>>>> origin/master
                try
                {
                    float left = (float)Math.Round(damge / 7);
                    Drawing.DrawRect(
                        position,
                        new Vector2(a, (float)(HUDInfo.GetHpBarSizeY(enemy))),
                        (enemy.Health > 0) ? new Color(150, 225, 150, 80) : new Color(70, 225, 150, 225));
                    Drawing.DrawRect(position, new Vector2(a, (float)(HUDInfo.GetHpBarSizeY(enemy))), Color.Black, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
         }
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
                rangeDisplay = null;
                target = null;
                Game.PrintMessage(
                    "<font face='Tahoma'>JE LastHit by JE loaded! </font>",
                    MessageType.LogMessage);
            }

            if (me == null || !me.IsValid)
            {
                isloaded = false;
                me = ObjectMgr.LocalHero;
                if (rangeDisplay == null)
                {
                    return;
                }
                target = null;
                rangeDisplay = null;

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

            if (Menu.Item("showatkrange").GetValue<bool>())
            {
                if (rangeDisplay == null)
                {
                    if (me.IsAlive)
                    {
                        rangeDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                        rangeDisplay.SetControlPoint(1, new Vector3(255, 80, 50));
                        rangeDisplay.SetControlPoint(3, new Vector3(20, 0, 0));
                        rangeDisplay.SetControlPoint(2, new Vector3(lastRange, 255, 0));
                    }
                }
                else
                {
                    if (!me.IsAlive)
                    {
                        rangeDisplay.Dispose();
                        rangeDisplay = null;
                    }
                    else if (lastRange != (me.GetAttackRange() + me.HullRadius + Menu.Item("outrange").GetValue<Slider>().Value))
                    {
                        rangeDisplay.Dispose();
                        rangeDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                        rangeDisplay.SetControlPoint(1, new Vector3(255, 80, 50));
                        rangeDisplay.SetControlPoint(3, new Vector3(15, 0, 0));
                        rangeDisplay.SetControlPoint(2, new Vector3(lastRange, 255, 0));
                    }
                }
            }
            else
            {
                rangeDisplay.Dispose();
                rangeDisplay = null;
            }
            


            if (target != null && (!target.IsValid || !target.IsVisible || !target.IsAlive || target.Health <= 0))
            {
                target = null;
            }
            var canCancel = Orbwalking.CanCancelAnimation();
            bool wait = false;
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

                    creepTarget = GetLowestHPCreep(me, null);
                    //creepTarget = GetAllLowestHPCreep(me);
                    creepTarget = KillableCreep(true, creepTarget,ref wait);
                }

                if (Game.IsKeyDown(Menu.Item("harass").GetValue<KeyBind>().Key)
                    && (creepTarget == null || !creepTarget.IsValid || !creepTarget.IsVisible /*|| !creepTarget.IsAlive*/
                        || creepTarget.Health <= 0 || !Orbwalking.AttackOnCooldown(creepTarget)))
                {
                    creepTarget = GetLowestHPCreep(me, null);
                    //creepTarget = GetAllLowestHPCreep(me);
                    creepTarget = KillableCreep(false, creepTarget,ref wait);
                    if (wait && /*Utils.SleepCheck("Orb") && Utils.SleepCheck("Orb2") &&*/ creepTarget!=null)
                    {
                        
                        Orbwalking.Orbwalk(creepTarget);
                        me.Hold();
                        //me.Attack(Game.MousePosition);
                        //Utils.Sleep(0.2, "Orb");
                        //Utils.Sleep(0.2, "Orb2");
                        
                    }
                    else if (!wait && creepTarget != null)
                    {
                        Orbwalking.Orbwalk(creepTarget);
                    }
                    
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
                    else if (Menu.Item("harassheroes").GetValue<bool>())
                    {
                        target = me.BestAATarget();
                       
                    }
                    else
                    { target = null; }
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
        public static Unit GetLowestHPCreep(Hero source, Unit markedcreep)
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
        private static Unit KillableCreep(bool islaneclear, Unit minion,ref bool wait)
        {
            //var minion = ObjectMgr.GetEntities<Creep>()
            //       .Where(creep => creep.IsAlive && me.Distance2D(creep) <= me.GetAttackRange())
            //       .OrderBy(creep => creep.Health).DefaultIfEmpty(null).FirstOrDefault();

            double test = 0;
            if (minion != null)
            {


                var missilespeed = GetProjectileSpeed(me);
                var time = me.IsRanged == false ? 0 : UnitDatabase.GetAttackBackswing(me) + (me.Distance2D(minion) / missilespeed);
                if (time >= minion.AttackSpeedValue)
                {
                    test = time * minion.AttacksPerSecond * minion.MinimumDamage;
                }
                


                // Console.WriteLine("test " + test + " time " + time + " distance " + me.Distance2D(minion) / missilespeed);
                if (minion != null && (minion.Health) < GetPhysDamageOnUnit(minion, test))
                {

                    if (me.CanAttack())
                    {
                        return minion;
                    }
                }
                if (Menu.Item("denied").GetValue<bool>())
                {
                    Unit minion2 = GetAllLowestHPCreep(me);
                    test = time * minion2.AttacksPerSecond * minion2.MinimumDamage;
                    if (minion2 != null && (minion2.Health) < GetPhysDamageOnUnit(minion2, test))
                    {

                        if (me.CanAttack())
                        {
                            return minion2;
                        }
                    }
                }
                //return null;
                if (minion != null && (minion.Health) >= GetPhysDamageOnUnit(minion, test) && (minion.Health) <= (GetPhysDamageOnUnit(minion, test) + me.MinimumDamage * 2))
                {
                    //if ((minion.Health) >= (GetPhysDamageOnUnit(minion, test) + me.MinimumDamage*1.5))
                    {
                       // wait = true;
                       // return minion;
                        return null;
                    }
                    //else
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
        private static void StopAndAttack()
        {
            me.Hold();
            me.Attack(Game.MousePosition);
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



        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
                return;
            if (Menu.Item("hpleftcreep").GetValue<bool>())
            {
                drawhpbar();
            }
            
        }
    }

}
