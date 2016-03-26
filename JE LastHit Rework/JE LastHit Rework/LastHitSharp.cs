namespace JE_LastHit_Rework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Extensions;
    using Ensage.Common.Menu;
    using Ensage.Common.Objects;
    using SharpDX;
    using SharpDX.Direct3D9;

    internal class LastHitSharp
    {
            #region Declare Static Fields

            public class DictionaryUnit
            {
                public Unit Unit { get; set; }

                public List<Ht> Ht { get; set; }

                public bool AHealth(Entity unit)
                {
                    if (unit.Handle != Unit.Handle) return false;
                    if (Ht.Any(x => x.Health - unit.Health < 10)) return true;
                    Ht.Add(new Ht { Health = unit.Health, Time = Game.GameTime, ACreeps = Attack(unit) });
                    return true;
                }
            }

            
            public class Ht
            {
                public float Health { get; set; }
                public float Time { get; set; }
                public int ACreeps { get; set; }
            }

            private static readonly Menu Menu = new Menu("LastHit", "lasthit", true);
            private static readonly int[] ult = { 0, 8, 9, 10 };
            private static Unit _creepTarget;
            private static Hero _target;
            private static Hero _me;
            private static Ability _q, _w, _e, _r;
            private static double _aPoint;
            private static int _outrange, _autoAttack, _autoAttackAfterSpell;
            private static bool _isloaded;
            private static List<DictionaryUnit> _creeps = new List<DictionaryUnit>();
            private static float _lastRange, _ias, _attackRange;
            private static ParticleEffect _rangeDisplay;
            private static readonly int[] qDamage = new int[5] { 0, 6, 7, 8, 9 };
            private static float scaleX, scaleY;
            #endregion

            public static void Init()
            {
                Menu.AddItem(new MenuItem("combatkey", "Chase mode").SetValue(new KeyBind(32, KeyBindType.Press)));
                Menu.AddItem(new MenuItem("harass", "Lasthit mode").SetValue(new KeyBind('C', KeyBindType.Press)));
                Menu.AddItem(new MenuItem("farmKey", "Farm mode").SetValue(new KeyBind('V', KeyBindType.Press)));
                Menu.AddItem(new MenuItem("kitekey", "Kite mode").SetValue(new KeyBind('H', KeyBindType.Press)));
                Menu.AddItem(new MenuItem("bonuswindup", "Bonus WindUp time on kitting").SetValue(new Slider(500, 100, 2000)));
                Menu.AddItem(new MenuItem("hpleftcreep", "Mark hp ?").SetValue(true));
                Menu.AddItem(new MenuItem("usespell", "Use spell ?").SetValue(true));
                Menu.AddItem(new MenuItem("harassheroes", "Harass in lasthit mode ?").SetValue(true));
                Menu.AddItem(new MenuItem("denied", "Deny creep ?").SetValue(true));
                Menu.AddItem(new MenuItem("AOC", "Atteck own creeps ?").SetValue(true));
                Menu.AddItem(new MenuItem("showatkrange", "Show attack range ?").SetValue(true));
                Menu.AddItem(new MenuItem("test", "Test Attack_Calc").SetValue(false));
                Menu.AddItem(new MenuItem("outrange", "Bonus range").SetValue(new Slider(100, 100, 500)));
                Menu.AddToMainMenu();
                scaleX = ((float)Drawing.Width / 1920);
                scaleY = ((float)Drawing.Height / 1080);
                Game.OnUpdate += Game_OnUpdate;
                Drawing.OnDraw += Drawing_OnDraw;
                Drawing.OnDraw += DrawUltiDamage;
                Game.OnUpdate += Attack_Calc;

                Orbwalking.Load();
            }
            #region TEST

            private static void Attack_Calc(EventArgs args)
            {
                if (!_isloaded || Game.IsPaused || Game.IsChatOpen) return;
                if (!Menu.Item("test").GetValue<bool>()) return;
                if (!ObjectManager.GetEntities<Unit>().Any(x => x.Distance2D(_me) <= 2000 && x.IsAlive && x.Health > 0)) return;
                var creeps = ObjectManager.GetEntities<Unit>().Where(x => x.Distance2D(_me) <= 2000 && x.IsAlive && x.Health > 0).ToList();
                foreach (var creep in creeps)
                {
                    if (!_creeps.Any(x => x.AHealth(creep)))
                    {
                        _creeps.Add(new DictionaryUnit { Unit = creep, Ht = new List<Ht>() });
                    }
                }
                //if (Utils.SleepCheck("Test"))
                //{
                //    foreach (var creep in _creeps)
                //    {
                //        Console.WriteLine("Unit : {0}", creep.Unit.Handle);
                //        foreach (var ht in creep.Ht)
                //        {
                //            Console.WriteLine("Health - time : {0} - {1} - {2}", ht.Health, ht.Time, ht.ACreeps);
                //        }
                //    }
                //    Utils.Sleep(2000, "Test");
                //}
                if (!Utils.SleepCheck("Clear")) return;
                creeps = ObjectManager.GetEntities<Unit>().Where(x => x.IsAlive).ToList();
                _creeps = (from creep in creeps
                           where _creeps.Any(x => x.Unit.Handle == creep.Handle)
                           select _creeps.Find(x => x.Unit.Handle == creep.Handle)).ToList();
                Utils.Sleep(10000, "Clear");
            }

            private static int Attack(Entity unit)
            {
                int t = 0;
                var upos = unit.Position;
                try
                {
                    var creeps =
                        ObjectManager.GetEntities<Unit>()
                            .Where(x => x.Distance2D(unit) <= x.AttackRange + 100 && x.IsAttacking() && x.IsAlive && x.Handle != unit.Handle && x.Team != unit.Team)
                            .ToList();
                    foreach (var creep in creeps)
                    {
                        var angle = creep.Rotation < 0 ? Math.Abs(creep.Rotation) : 360 - creep.Rotation;
                        if (Math.Abs(angle - creep.FindAngleForTurnTime(unit.Position)) <= 3) t++;
                    }
                }
                catch (Exception)
                {
                }
                return t;
            }

            public static double Healthpredict(Unit unit, double time)
            {
                if (_creeps.Any(x => x.Unit.Handle == unit.Handle) && Menu.Item("test").GetValue<bool>())
                {
                    try
                    {
                        var hta = _creeps.First(x => x.Unit.Handle == unit.Handle).Ht.ToArray();
                        var length = hta.Length - 1;
                        if ((hta.Length - hta[length].ACreeps) >= 0)
                        {
                            var aCreeps = hta[length].ACreeps;

                            if (time <=
                                hta[length - aCreeps + 1].Time -
                                hta[length - aCreeps].Time)
                            {
                                return hta[length - aCreeps].Health -
                                       hta[length - aCreeps + 1].Health - 10;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //
                    }
                }
                return 0;
            }

            #endregion

            #region OnGameUpdate

            private static void Game_OnUpdate(EventArgs args)
            {
                
                if (!_isloaded)
                {
                    _me = ObjectManager.LocalHero;
                    if (!Game.IsInGame || _me == null)
                    {
                        return;
                    }
                    // Auto Attack Checker
                    //_autoAttackAfterSpell = Game.GetConsoleVar("dota_player_units_auto_attack_after_spell").GetInt();
                    //_autoAttack = Game.GetConsoleVar("dota_player_units_auto_attack").GetInt();
                    // Auto Attack Checker
                    _lastRange = _me.GetAttackRange();
                    _isloaded = true;
                    _rangeDisplay = null;
                    _target = null;
                   
                }

                if (_me == null || !_me.IsValid)
                {
                    _isloaded = false;
                    _me = ObjectManager.LocalHero;
                    if (_rangeDisplay == null)
                    {
                        return;
                    }
                    _rangeDisplay = null;
                    _target = null;
                    return;
                }

                if (Game.IsPaused || Game.IsChatOpen)
                {
                    return;
                }

                if (_target != null && (!_target.IsValid || !_target.IsVisible || !_target.IsAlive || _target.Health <= 0))
                {
                    _target = null;
                }
               
                _q = _me.Spellbook.SpellQ;
                _w = _me.Spellbook.SpellW;
                _e = _me.Spellbook.SpellE;
                _r = _me.Spellbook.SpellR;
                
                double apoint = 0;
                apoint = _me.ClassID == ClassID.CDOTA_Unit_Hero_ArcWarden
                    ? 0.3
                    : UnitDatabase.Units.Find(x => x.UnitName == _me.Name).AttackPoint;
                _aPoint = apoint / (1 + _me.AttacksPerSecond * _me.BaseAttackTime / 100) * 500;
                _outrange = Menu.Item("outrange").GetValue<Slider>().Value;

                if (_me.ClassID == ClassID.CDOTA_Unit_Hero_TrollWarlord)
                    _attackRange = _q.IsToggled ? 128 : _me.GetAttackRange();
                else
                    _attackRange = _me.GetAttackRange();

                var wait = false;
                if (_target != null && !_target.IsVisible && !Orbwalking.AttackOnCooldown(_target))
                {
                    _target = _me.ClosestToMouseTarget(500);
                }
                else if (_target == null || !Orbwalking.AttackOnCooldown(_target))
                {
                    var bestAa = _me.BestAATarget();
                    if (bestAa != null)
                    {
                        _target = _me.BestAATarget();
                    }
                }
         
                if (Menu.Item("showatkrange").GetValue<bool>())
                {
                    if (_rangeDisplay == null)
                    {
                        if (_me.IsAlive)
                        {
                            _rangeDisplay = _me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                            _rangeDisplay.SetControlPoint(1, new Vector3(255, 80, 50));
                            _rangeDisplay.SetControlPoint(3, new Vector3(20, 0, 0));
                            _rangeDisplay.SetControlPoint(2, new Vector3(_lastRange, 255, 0));
                        }
                    }
                    else
                    {
                        if (!_me.IsAlive)
                        {
                            _rangeDisplay.Dispose();
                            _rangeDisplay = null;
                        }
                        else if (_lastRange != _attackRange)
                        {
                            _rangeDisplay.Dispose();
                            _lastRange = _attackRange;
                            _rangeDisplay = _me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                            _rangeDisplay.SetControlPoint(1, new Vector3(255, 80, 50));
                            _rangeDisplay.SetControlPoint(3, new Vector3(15, 0, 0));
                            _rangeDisplay.SetControlPoint(2, new Vector3(_lastRange, 255, 0));
                        }
                    }
                }
                else
                {
                    if (_rangeDisplay != null) _rangeDisplay.Dispose();
                    _rangeDisplay = null;
                }

                if (Game.IsKeyDown(Menu.Item("harass").GetValue<KeyBind>().Key) && Utils.SleepCheck("cast"))
                {
                    Autoattack(0, 0);
                    _creepTarget = GetLowestHpCreep(_me, null);
                    _creepTarget = KillableCreep(false, _creepTarget, ref wait);
                    Console.WriteLine(_me.MinimumDamage + qDamage[_q.Level] * _me.MaximumMana / 100+"-"+_q.GetDamage(1).ToString());
                    if (Menu.Item("usespell").GetValue<bool>() && Utils.SleepCheck("cooldown"))
                        SpellCast();
                    if (_creepTarget != null && _creepTarget.IsValid && _creepTarget.IsVisible && _creepTarget.IsAlive)
                    {
                        var time = _me.IsRanged == false
                        ? _aPoint * 2 / 1000 + _me.GetTurnTime(_creepTarget.Position)
                        : _aPoint * 2 / 1000 + _me.GetTurnTime(_creepTarget.Position) + _me.Distance2D(_creepTarget) / GetProjectileSpeed(_me);

                        var time2 = _me.IsRanged == false
                        ? _aPoint / 1000
                        : _aPoint / 1000 + _me.Distance2D(_creepTarget) / GetProjectileSpeed(_me);

                        var getDamage = GetDamageOnUnit(_creepTarget, Healthpredict(_creepTarget, time));
                        var getDamage2 = GetDamageOnUnit(_creepTarget, Healthpredict(_creepTarget, time2));
                        if (_creepTarget.Distance2D(_me) <= _attackRange && _creepTarget.Health < getDamage * 2)
                        {
                            if (_creepTarget.Health < getDamage ||
                                     (_creepTarget.Health < getDamage && _creepTarget.Team == _me.Team &&
                                      Menu.Item("denied").GetValue<bool>()))
                            {
                                if (!_me.IsAttacking())
                                    _me.Attack(_creepTarget);
                            }
                            else if (_creepTarget.Health < getDamage * 2 && _creepTarget.Health >= getDamage &&
                                _creepTarget.Team != _me.Team && Utils.SleepCheck("stop"))
                            {
                                _me.Hold();
                                _me.Attack(_creepTarget);
                                Utils.Sleep(_aPoint + Game.Ping, "stop");
                            }
                        }
                        else //if (_me.Distance2D(_creepTarget) >= _attackRange && Utils.SleepCheck("walk"))
                        {
                          
                          
                            _target = _me.BestAATarget();
                            if (_target != null)
                            {
                                if (Menu.Item("harassheroes").GetValue<bool>() && _target.Distance2D(_me) <= _attackRange)
                                    _target = _me.BestAATarget();
                                else
                                    _target = null;

                                if (Menu.Item("usespell").GetValue<bool>() && Utils.SleepCheck("cooldown") && _target != null)
                                {
                                    _q.UseAbility(_target);
                                    Utils.Sleep(_q.GetCastPoint(_q.Level) * 300 + 50 + Game.Ping, "cast");
                                    Utils.Sleep(_q.GetCooldown(_q.Level) * 300 + 50 + Game.Ping, "cooldown");
                                }
                                else
                                {
                                    Orbwalking.Orbwalk(_target);
                                   // return;
                                }
                            }
                            //Orbwalking.Orbwalk(_target);
                           
                        }
                    }
                    else
                    {
                        _target = _me.BestAATarget();
                        if (_target != null)
                        {
                            if (Menu.Item("harassheroes").GetValue<bool>() && _target.Distance2D(_me) <= _attackRange)
                                _target = _me.BestAATarget();
                            else
                                _target = null;
                        }
                        Orbwalking.Orbwalk(_target);
                    }
                }
                else if (Game.IsKeyDown(Menu.Item("farmKey").GetValue<KeyBind>().Key) && Utils.SleepCheck("cast"))
                {
                    Autoattack(0, 0);
                    _creepTarget = GetLowestHpCreep(_me, null);
                    _creepTarget = KillableCreep(true, _creepTarget, ref wait);
                    if (_creepTarget != null && _creepTarget.IsValid && _creepTarget.IsVisible && _creepTarget.IsAlive)
                    {

                        if (Menu.Item("usespell").GetValue<bool>() && Utils.SleepCheck("cooldown"))
                            SpellCast();

                    }
                    Orbwalking.Orbwalk(_creepTarget);
                }
                else if (Game.IsKeyDown(Menu.Item("combatkey").GetValue<KeyBind>().Key))
                {
                    Orbwalking.Orbwalk(_target, attackmodifiers: true);
                }
                else if (Game.IsKeyDown(Menu.Item("kitekey").GetValue<KeyBind>().Key))
                {
                    Orbwalking.Orbwalk(
                        _target,
                        attackmodifiers: true,
                        bonusWindupMs: Menu.Item("bonusWindup").GetValue<Slider>().Value);
                }
                else
                {
                    Autoattack(_autoAttack, _autoAttackAfterSpell);
                }
            }
            private static void DrawUltiDamage(EventArgs args)
            {
                if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
                {
                    return;
                }

                _me = ObjectMgr.LocalHero;
                if (_me == null || _me.ClassID != ClassID.CDOTA_Unit_Hero_Obsidian_Destroyer) return;

                var ultLvl = _me.Spellbook.SpellR.Level;
                var enemy =
                    ObjectMgr.GetEntities<Hero>()
                        .Where(y => y.Team != _me.Team && y.IsAlive && y.IsVisible && !y.IsIllusion)
                        .ToList();

                foreach (var v in enemy)
                {
                    if (!v.IsVisible || !v.IsAlive) continue;

                    var meInt = Math.Floor(_me.TotalIntelligence);
                    var enemyInt = Math.Floor(v.TotalIntelligence);
                    var damage = Math.Floor((ult[ultLvl] * (meInt - enemyInt)) * (1 - v.MagicDamageResist));
                    var dmg = v.Health - damage;
                    var canKill = dmg <= 0;

                    var screenPos = HUDInfo.GetHPbarPosition(v);
                    if (!OnScreen(v.Position)) continue;

                    var text = canKill ? "Yes" : "No, damage:" + Math.Floor(damage);
                    var size = new Vector2(15, 15);
                    var textSize = Drawing.MeasureText(text, "Arial", size, FontFlags.AntiAlias);
                    var position = new Vector2(screenPos.X - textSize.X - 2, screenPos.Y - 3);
                    Drawing.DrawText(
                        text,
                        position,
                        size,
                        (canKill ? Color.LawnGreen : Color.Red),
                        FontFlags.AntiAlias);

                }

            }
            private static bool OnScreen(Vector3 v)
            {
                return !(Drawing.WorldToScreen(v).X < 0 || Drawing.WorldToScreen(v).X > Drawing.Width || Drawing.WorldToScreen(v).Y < 0 || Drawing.WorldToScreen(v).Y > Drawing.Height);
            }

            private static void Drawhpbar()
            {
                try
                {
                    var enemies =
                    ObjectManager.GetEntities<Unit>()
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
                                && x.Team != _me.Team && x.Distance2D(_me) < _attackRange + 5000);
                    foreach (var enemy in enemies)
                    {
                        var health = enemy.Health;
                        var maxHealth = enemy.MaximumHealth;
                        if (health == maxHealth) continue;
                        var damge = (float)GetDamageOnUnit(enemy, 0);
                        var hpleft = health;
                        var hpperc = hpleft / maxHealth;

                        var hbarpos = HUDInfo.GetHPbarPosition(enemy);

                        Vector2 screenPos;
                        var enemyPos = enemy.Position + new Vector3(0, 0, enemy.HealthBarOffset);
                        if (!Drawing.WorldToScreen(enemyPos, out screenPos)) continue;

                        var start = screenPos;

                        hbarpos.X = start.X - HUDInfo.GetHPBarSizeX(enemy) / 2;
                        hbarpos.Y = start.Y;
                        var hpvarx = hbarpos.X;
                        var a = (float)Math.Round(damge * HUDInfo.GetHPBarSizeX(enemy) / enemy.MaximumHealth);
                        var position = hbarpos + new Vector2(hpvarx * hpperc + 10, -12);
                        Drawing.DrawRect(
                            position,
                            new Vector2(a, HUDInfo.GetHpBarSizeY(enemy) - 4),
                            enemy.Health > damge
                                ? enemy.Health > damge * 2 ? new Color(180, 205, 205, 40) : new Color(255, 0, 0, 60)
                                : new Color(127, 255, 0, 80));
                        Drawing.DrawRect(position, new Vector2(a, HUDInfo.GetHpBarSizeY(enemy) - 4), Color.Black, true);
                        //if (!Menu.Item("test").GetValue<bool>()) continue;
                        var time = _me.IsRanged == false
                        ? _aPoint * 2 / 1000 + _me.GetTurnTime(_creepTarget.Position)
                        : _aPoint * 2 / 1000 + _me.GetTurnTime(enemy.Position) + _me.Distance2D(enemy) / GetProjectileSpeed(_me);
                        var damgeprediction = Healthpredict(enemy, time);
                        var b = (float)Math.Round(damgeprediction * 1 * HUDInfo.GetHPBarSizeX(enemy) / enemy.MaximumHealth);
                        var position2 = position + new Vector2(a, 0);
                        Drawing.DrawRect(position2, new Vector2(b, HUDInfo.GetHpBarSizeY(enemy) - 4), Color.YellowGreen);
                        Drawing.DrawRect(position2, new Vector2(b, HUDInfo.GetHpBarSizeY(enemy) - 4), Color.Black, true);
                    }
                }
                catch (Exception)
                {
                    //
                }


                var ultLvl = _me.Spellbook.SpellR.Level;
            var _drenemy =
                ObjectMgr.GetEntities<Hero>()
                    .Where(y => y.Team != _me.Team && y.IsAlive && y.IsVisible && !y.IsIllusion)
                    .ToList();

           /* foreach (var x in _drenemy)
            {
                //Console.WriteLine(1);
                var health = x.Health;
                var maxHealth = x.MaximumHealth;

                var meInt = Math.Floor(_me.TotalIntelligence);
                var enemyInt = Math.Floor(x.TotalIntelligence);
                var damge = Math.Floor((ult[ultLvl] * (meInt - enemyInt)) * (1 - x.MagicDamageResist));
                var hpleft = health;
                var hpperc = hpleft / maxHealth;

                var dmgperc = Math.Min(damge, health) / maxHealth;
                Vector2 hbarpos;
                hbarpos = HUDInfo.GetHPbarPosition(x);

                Vector2 screenPos;
                var enemyPos = x.Position + new Vector3(0, 0, x.HealthBarOffset);
                if (!Drawing.WorldToScreen(enemyPos, out screenPos)) continue;

                var start = screenPos;


                hbarpos.X = start.X + (HUDInfo.GetHPBarSizeX(x) / 2);
                hbarpos.Y = start.Y;
                var hpvarx = hbarpos.X;
                var hpbary = hbarpos.Y;
                float a = (float)Math.Round((damge * HUDInfo.GetHPBarSizeX(x)) / (x.MaximumHealth));
                var position = hbarpos - new Vector2(a, 32 * scaleY);

                //Console.WriteLine("damage" + damge.ToString());

                try
                {
                    float left = (float)Math.Round(damge / 7);
                    Drawing.DrawRect(
                        position,
                        new Vector2(a, (float)(HUDInfo.GetHpBarSizeY(x))),
                        (x.Health > 0) ? new Color(150, 225, 150, 80) : new Color(70, 225, 150, 225));
                    Drawing.DrawRect(position, new Vector2(a, (float)(HUDInfo.GetHpBarSizeY(x))), Color.Black, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }*/
            }

            #endregion

            #region predict

            private static void Autoattack(int aa, int aas)
            {
               /* if (Game.GetConsoleVar("dota_player_units_auto_attack").GetInt() == aa &&
                    Game.GetConsoleVar("dota_player_units_auto_attack_after_spell").GetInt() == aas)
                    return;
                Game.ExecuteCommand("dota_player_units_auto_attack " + aa);
                Game.ExecuteCommand("dota_player_units_auto_attack_after_spell " + aas);*/
            }

            private static void SpellCast()
            {
                try
                {
                    var creeps =
                        ObjectManager.GetEntities<Unit>()
                            .Where(
                                x =>
                                    (x.ClassID == ClassID.CDOTA_BaseNPC_Tower ||
                                     x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creep
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Additive
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Barracks
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Building
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creature) && x.IsAlive && x.IsVisible
                                    && x.Team != _me.Team && x.Distance2D(_me) < 800 + _outrange)
                            .OrderByDescending(creep => creep.Health);
                    foreach (var creep in creeps)
                    {
                        double damage = 0;
                        switch (_me.ClassID)
                        {
                            case ClassID.CDOTA_Unit_Hero_Obsidian_Destroyer:
                                if (_q.Level > 0 && _q.CanBeCasted())
                                {
                                    damage = Math.Floor(((_me.MinimumDamage + qDamage[_q.Level] * _me.MaximumMana / 100) * (1 - creep.MagicDamageResist)) - (creep.HealthRegeneration));
                                    if (creep.Health < damage && _me.Distance2D(creep) <= _q.CastRange)
                                    {
                                        _q.UseAbility(creep);
                                        Utils.Sleep(_q.GetCastPoint(_q.Level) * 300 + 50 + Game.Ping, "cast");
                                        Utils.Sleep(_q.GetCooldown(_q.Level) * 300 + 50 + Game.Ping, "cooldown");
                                    }
                                    //damage = ((_q.Level - 1) * 15 + 85) * (1 - creep.MagicDamageResist);
                                    //if (damage > creep.Health)
                                    //{
                                    //    _q.UseAbility(creep);
                                    //    Utils.Sleep(_q.GetCastPoint(_q.Level) * 1000 + 50 + Game.Ping, "cast");
                                    //    Utils.Sleep(_q.GetCooldown(_q.Level) * 1000 + 50 + Game.Ping, "cooldown");
                                    //}
                                }
                                break;
                            case ClassID.CDOTA_Unit_Hero_Zuus:
                                if (_q.Level > 0 && _q.CanBeCasted() && _me.Distance2D(creep) > _attackRange)
                                {
                                    damage = ((_q.Level - 1) * 15 + 85) * (1 - creep.MagicDamageResist);
                                    if (damage > creep.Health)
                                    {
                                        _q.UseAbility(creep);
                                        Utils.Sleep(_q.GetCastPoint(_q.Level) * 1000 + 50 + Game.Ping, "cast");
                                        Utils.Sleep(_q.GetCooldown(_q.Level) * 1000 + 50 + Game.Ping, "cooldown");
                                    }
                                }
                                break;
                            case ClassID.CDOTA_Unit_Hero_Bristleback:
                                if (_w.Level > 0 && _w.CanBeCasted() && _me.Distance2D(creep) > _attackRange)
                                {
                                    double quillSprayDmg = 0;
                                    if (
                                        creep.Modifiers.Any(
                                            x =>
                                                x.Name == "modifier_bristleback_quill_spray_stack" ||
                                                x.Name == "modifier_bristleback_quill_spray"))
                                        quillSprayDmg =
                                            creep.Modifiers.Find(
                                                x =>
                                                    x.Name == "modifier_bristleback_quill_spray_stack" ||
                                                    x.Name == "modifier_bristleback_quill_spray").StackCount * 30 +
                                            (_w.Level - 1) * 2;
                                    damage = ((_w.Level - 1) * 20 + 20 + quillSprayDmg) *
                                             (1 - 0.06 * creep.Armor / (1 + 0.06 * creep.Armor));
                                    if (damage > creep.Health && _w.CastRange < _me.Distance2D(creep))
                                    {
                                        _w.UseAbility();
                                        Utils.Sleep(_w.GetCastPoint(_w.Level) * 1000 + 50 + Game.Ping, "cast");
                                        Utils.Sleep(_w.GetCooldown(_w.Level) * 1000 + 50 + Game.Ping, "cooldown");
                                    }
                                }
                                break;
                            case ClassID.CDOTA_Unit_Hero_PhantomAssassin:
                                if (_q.Level > 0 && _q.CanBeCasted() && _me.Distance2D(creep) > _attackRange)
                                {
                                    var time = 300 + _me.Distance2D(creep) / _q.GetProjectileSpeed();
                                    if (time < creep.SecondsPerAttack * 1000)
                                        damage = ((_q.Level - 1) * 40 + 60) * (1 - 0.06 * creep.Armor / (1 + 0.06 * creep.Armor));
                                    if (damage > creep.Health)
                                    {
                                        _q.UseAbility(creep);
                                        Utils.Sleep(_q.GetCastPoint(_q.Level) * 1000 + 50 + Game.Ping, "cast");
                                        Utils.Sleep(6 * 1000 + Game.Ping, "cooldown");
                                    }
                                }
                                break;
                            case ClassID.CDOTA_Unit_Hero_Pudge:
                                if (_w.Level > 0)
                                {
                                    if (_creepTarget != null && creep.Handle == _creepTarget.Handle && _me.Distance2D(creep) <= _attackRange)
                                    {
                                        damage = GetDamageOnUnit(creep, 0);
                                        if (damage > creep.Health && !_w.IsToggled && _me.IsAttacking())
                                        {
                                            _w.ToggleAbility();
                                            Utils.Sleep(200 + Game.Ping, "cooldown");
                                        }
                                    }
                                    if (_w.IsToggled)
                                    {
                                        _w.ToggleAbility();
                                        Utils.Sleep(_aPoint * 2 + Game.Ping, "cooldown");
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (Exception)
                {
                    //
                }
            }

            public static Unit GetAllLowestHpCreep(Hero source)
            {
                try
                {
                    var attackRange = source.GetAttackRange();
                    var lowestHp =
                        ObjectManager.GetEntities<Unit>()
                            .Where(
                                x =>
                                    (x.ClassID == ClassID.CDOTA_BaseNPC_Tower ||
                                     x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creep
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Additive
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Barracks
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Building
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creature) && x.IsAlive && x.IsVisible
                                     && x.Distance2D(source) < attackRange + _outrange)
                            .OrderBy(creep => creep.Health)
                            .DefaultIfEmpty(null)
                            .FirstOrDefault();
                    return lowestHp;
                }
                catch (Exception)
                {
                    Console.WriteLine("Error GetLowestHpCreep");
                }
                return null;
            }

            public static Unit GetLowestHpCreep(Hero source, Unit markedcreep)
            {
                try
                {
                    var lowestHp =
                        ObjectManager.GetEntities<Unit>()
                            .Where(
                                x =>
                                    (x.ClassID == ClassID.CDOTA_BaseNPC_Tower ||
                                     x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creep
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Additive
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Barracks
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Building
                                     || x.ClassID == ClassID.CDOTA_BaseNPC_Creature) && x.IsAlive && x.IsVisible
                                    && x.Team != source.Team && x.Distance2D(source) < _attackRange + _outrange &&
                                    x != markedcreep)
                            .OrderBy(creep => creep.Health)
                            .DefaultIfEmpty(null)
                            .FirstOrDefault();
                    return lowestHp;
                }
                catch (Exception)
                {
                    //
                }
                return null;
            }

            private static Unit KillableCreep(bool islaneclear, Unit minion, ref bool wait)
            {
                try
                {
                    var missilespeed = GetProjectileSpeed(_me);
                    var time = _me.IsRanged == false
                        ? 0
                        : _aPoint * 2 + _me.GetTurnTime(minion.Position) + _me.Distance2D(minion) / missilespeed;
                    double test = 0;
                    if (time >= minion.SecondsPerAttack)
                    {
                        test = time * minion.AttacksPerSecond * minion.MinimumDamage;
                    }
                    if (Menu.Item("test").GetValue<bool>())
                        test = 0;
                    if (minion.Health < GetDamageOnUnit(_creepTarget, test) * 2 &&
                        minion.Health / minion.MaximumHealth < 0.75)
                    {
                        if (_me.CanAttack())
                            return minion;
                    }
                    if (islaneclear)
                    {
                        //if (minion != null && (minion.Health) >= GetDamageOnUnit(minion, test) && (minion.Health) <= (GetDamageOnUnit(minion, test) + _me.MinimumDamage ))
                        if (minion != null && (minion.Health) < (GetDamageOnUnit(minion, test)*2 + _me.MinimumDamage))
                        {
                            var minion2 = minion;// GetAllLowestHpCreep(_me);
                            test = time * minion2.AttacksPerSecond * minion2.MinimumDamage;
                            if (Menu.Item("test").GetValue<bool>())
                                test = 0;
                            if (minion != null && (minion.Health) < GetDamageOnUnit(minion, test))
                            {
                                Console.WriteLine("2");
                                if (_me.CanAttack())
                                    return minion2;
                            }
                            return null;
                            ////if (!Menu.Item("AOC").GetValue<bool>()) return null;
                            //{
                                
                            //    if (!(minion2.Health > GetDamageOnUnit(minion2, test)) ||  minion2.Health >= minion2.MaximumHealth / 2 || minion2.Team != _me.Team)
                            //        return null;
                            //    if (_me.CanAttack())
                            //        return minion2;
                            //}
                            //return null;
                        }
                        Console.WriteLine("1");
                        return minion;
                    }

                    if (Menu.Item("denied").GetValue<bool>())
                    {
                        var minion2 = GetAllLowestHpCreep(_me);
                        test = time * minion2.AttacksPerSecond * minion2.MinimumDamage;
                        if (Menu.Item("test").GetValue<bool>())
                            test = 0;
                        if (minion2.Health < GetDamageOnUnit(minion2, test) * 1.5 && minion2.Team == _me.Team)
                        {
                            if (_me.CanAttack())
                                return minion2;
                        }
                    }


                }
                catch (Exception)
                {
                    //
                }
                return null;
            }

            private static double GetDamageOnUnit(Unit unit, double bonusdamage)
            {
                var quellingBlade = _me.FindItem("item_quelling_blade");
                double modif = 1;
                double magicdamage = 0;
                double physDamage = _me.MinimumDamage + _me.BonusDamage;
                if (quellingBlade != null && unit.Team != _me.Team)
                {
                    if (_me.IsRanged)
                    {
                        physDamage = _me.MinimumDamage * 1.15 + _me.BonusDamage;
                    }
                    else
                    {
                        physDamage = _me.MinimumDamage * 1.4 + _me.BonusDamage;
                    }
                }
                double bonusdamage2 = 0;
                switch (_me.ClassID)
                {
                    case ClassID.CDOTA_Unit_Hero_AntiMage:
                        if (unit.MaximumMana > 0 && unit.Mana > 0 && _q.Level > 0 && unit.Team != _me.Team)
                            bonusdamage2 = (_q.Level - 1) * 12 + 28 * 0.6;
                        break;
                    case ClassID.CDOTA_Unit_Hero_Viper:
                        if (_w.Level > 0 && unit.Team != _me.Team)
                        {
                            var nethertoxindmg = _w.Level * 2.5;
                            //var percent = Math.Floor((double) unit.Health / unit.MaximumHealth * 100);
                            //if (percent > 80 && percent <= 100)
                            //    bonusdamage2 = nethertoxindmg * 0.5;
                            //else if (percent > 60 && percent <= 80)
                            //    bonusdamage2 = nethertoxindmg * 1;
                            //else if (percent > 40 && percent <= 60)
                            //    bonusdamage2 = nethertoxindmg * 2;
                            //else if (percent > 20 && percent <= 40)
                            //    bonusdamage2 = nethertoxindmg * 4;
                            //else if (percent > 0 && percent <= 20)
                            //    bonusdamage2 = nethertoxindmg * 8;
                        }
                        break;
                    case ClassID.CDOTA_Unit_Hero_Ursa:
                        var furymodif = 0;
                        if (_me.Modifiers.Any(x => x.Name == "modifier_ursa_fury_swipes_damage_increase"))
                            furymodif = unit.Modifiers.Find(x => x.Name == "modifier_ursa_fury_swipes_damage_increase").StackCount;
                        if (_e.Level > 0)
                        {
                            if (furymodif > 0)
                                bonusdamage2 = furymodif * ((_e.Level - 1) * 5 + 15);
                            else
                                bonusdamage2 = (_e.Level - 1) * 5 + 15;
                        }
                        break;
                    case ClassID.CDOTA_Unit_Hero_BountyHunter:
                        if (_w.Level > 0 && _w.AbilityState == AbilityState.Ready)
                            bonusdamage2 = physDamage * ((_w.Level - 1) * 0.25 + 0.50);
                        break;
                    case ClassID.CDOTA_Unit_Hero_Weaver:
                        if (_e.Level > 0 && _e.AbilityState == AbilityState.Ready)
                            bonusdamage2 = physDamage;
                        break;
                    case ClassID.CDOTA_Unit_Hero_Kunkka:
                        if (_w.Level > 0 && _w.AbilityState == AbilityState.Ready && _w.IsAutoCastEnabled)
                            bonusdamage2 = (_w.Level - 1) * 15 + 25;
                        break;
                    case ClassID.CDOTA_Unit_Hero_Juggernaut:
                        if (_e.Level > 0)
                            if (_me.NetworkActivity == NetworkActivity.Crit)
                                bonusdamage2 = physDamage;
                        break;
                    case ClassID.CDOTA_Unit_Hero_Brewmaster:
                        if (_e.Level > 0)
                            if (_me.NetworkActivity == NetworkActivity.Crit)
                                bonusdamage2 = physDamage;
                        break;
                    case ClassID.CDOTA_Unit_Hero_ChaosKnight:
                        if (_e.Level > 0)
                            if (_me.NetworkActivity == NetworkActivity.Crit)
                                bonusdamage2 = physDamage * ((_e.Level - 1) * 0.5 + 0.25);
                        break;
                    case ClassID.CDOTA_Unit_Hero_SkeletonKing:
                        if (_e.Level > 0)
                            if (_me.NetworkActivity == NetworkActivity.Crit)
                                bonusdamage2 = physDamage * ((_e.Level - 1) * 0.5 + 0.5);
                        break;
                    case ClassID.CDOTA_Unit_Hero_Life_Stealer:
                        if (_w.Level > 0)
                            bonusdamage2 = unit.Health * ((_w.Level - 1) * 0.01 + 0.045);
                        break;
                    case ClassID.CDOTA_Unit_Hero_PhantomAssassin:
                        if (_r.Level > 0)
                            if (_me.NetworkActivity == NetworkActivity.Crit)
                                bonusdamage2 = physDamage * ((_r.Level - 1) * 1.1 + 1.3);
                        break;
                }

                if (_me.Modifiers.Any(x => x.Name == "modifier_bloodseeker_bloodrage"))
                {
                    modif = modif *
                            (ObjectManager.GetEntities<Hero>()
                                .First(x => x.ClassID == ClassID.CDOTA_Unit_Hero_Bloodseeker)
                                .Spellbook.Spell1.Level - 1) * 0.05 + 1.25;
                }
                if (_me.Modifiers.Any(x => x.Name == "modifier_storm_spirit_overload"))
                {
                    magicdamage = magicdamage + (_e.Level - 1) * 20 + 30;
                }
                if (_me.Modifiers.Any(x => x.Name == "modifier_chilling_touch"))
                {
                    magicdamage = magicdamage + (_e.Level - 1) * 20 + 50;
                }
                if (_me.ClassID == ClassID.CDOTA_Unit_Hero_Pudge && _w.Level > 0 && Menu.Item("usespell").GetValue<bool>() &&
                    _me.Distance2D(unit) <= _attackRange)
                {
                    magicdamage = magicdamage + (_w.Level - 1) * 6 + 6;
                }

                bonusdamage = bonusdamage + bonusdamage2;
                var damageMp = 1 - 0.06 * unit.Armor / (1 + 0.06 * Math.Abs(unit.Armor));
                magicdamage = magicdamage * (1 - unit.MagicDamageResist);

                var realDamage = ((bonusdamage + physDamage) * damageMp + magicdamage) * modif;
                if (unit.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                    unit.ClassID == ClassID.CDOTA_BaseNPC_Tower)
                {
                    realDamage = realDamage / 2;
                }
                if (realDamage > unit.MaximumHealth)
                    realDamage = unit.MaximumHealth;

                return realDamage;
            }

            public static float GetProjectileSpeed(Hero unit)
            {
                return (_me.ClassID == ClassID.CDOTA_Unit_Hero_ArcWarden
                    ? 800
                    : UnitDatabase.GetByName(unit.Name).ProjectileSpeed);
            }

            #endregion

            private static void Drawing_OnDraw(EventArgs args)
            {
                if (!Game.IsInGame)
                    return;
                if (Menu.Item("hpleftcreep").GetValue<bool>())
                {
                    Drawhpbar();
                }
                //var units = ObjectManager.GetEntities<Unit>().Where(x => x.Distance2D(_me) < 1000);
                //foreach (var unit in units)
                //{
                //    var upos = Drawing.WorldToScreen(unit.Position);
                //    Drawing.DrawText(_me.NetworkActivity + " ", "", new Vector2(upos.X - 40, upos.Y - 20), new Vector2(30), Color.White, FontFlags.Outline);
                //}
                //try
                //{
                //    //Drawing.DrawText( + " ", "", new Vector2(_me.Position.X, _me.Position.Y),new Vector2(40), Color.AliceBlue, FontFlags.Outline);
                //    var pos = Drawing.WorldToScreen(Game.MousePosition);
                //    //Drawing.DrawText("   " + _me.Rotation , "", new Vector2(pos.X, pos.Y - 20), new Vector2(30), Color.White, FontFlags.Outline);
                //    var units = ObjectManager.GetEntities<Unit>().Where(x => x.Distance2D(_me) < 1000);
                //    //Color[] color = new Color[]();
                //    foreach (var unit in units)
                //    {
                //        var upos = Drawing.WorldToScreen(unit.Position);
                //        var creeps =
                //            ObjectManager.GetEntities<Unit>()
                //                .Where(x => x.Distance2D(unit) <= x.AttackRange + 50 && x.IsAttacking() && x.IsAlive && x.Handle != unit.Handle && x.Team != unit.Team)
                //                .ToList();
                //        foreach (var creep in creeps)
                //        {
                //            var angle = Utils.RadianToDegree(creep.RotationRad < 0 ? Math.Abs(creep.RotationRad) : Math.PI * 2 - creep.RotationRad);
                //            if (Math.Abs(creep.FindAngleForTurnTime(unit.Position) - angle) <= 3)
                //            {
                //                var cpos = Drawing.WorldToScreen(creep.Position);
                //                Drawing.DrawLine(upos, cpos, Color.Gainsboro);
                //                Drawing.DrawText(Attack(unit).ToString(), "", new Vector2(cpos.X - 40, cpos.Y - 20), new Vector2(30), Color.White, FontFlags.Outline);
                //            }
                //        }
                //    }
                //}
                //catch (Exception)
                //{
                //    //
                //}
            }
        }
    }


