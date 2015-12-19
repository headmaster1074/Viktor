﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;

namespace Gangplank
{
    public class Barrel
    {
        public Obj_AI_Minion barrel;
        public float time;

        public Barrel(Obj_AI_Minion objAiBase, int tickCount)
        {
            barrel = objAiBase;
            time = tickCount;
        }
    }

    public class Program
    {
        public static Menu Config;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static IEnumerable<Obj_AI_Minion> savedbarrels;
        public static List<Vector3> barrelpoints = new List<Vector3>();
        // Spells
        private static Spell Q, W, E, R;
        private const int BarrelExplosionRange = 350;
        private const int BarrelConnectionRange = BarrelExplosionRange * 2 - 20;
        public static Orbwalking.Orbwalker Orbwalker;

        public static Vector3 acoords;
        public static Vector3 bcoords;
        public static void Main(string[] args)
        {


            // Register events
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        public class EDelay
        {
            public static Vector3 position;
            public static int time;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Champ validation
            if (player.ChampionName != "Gangplank")
                return;
            // Define spells
            Q = new Spell(SpellSlot.Q, 600f); //2600f
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1000f);
            E.SetSkillshot(0.8f, 50, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R);
            R.SetSkillshot(1f, 100, float.MaxValue, false, SkillshotType.SkillshotCircle);
            SetupMenu();
            // Register events
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            savedbarrels = GetBarrels();
            Orbwalker.SetAttack(true);
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();

            };
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                Farm();
            };

            if (Config.Item("coorda").GetValue<KeyBind>().Active)
            {
                acoords = Game.CursorPos;
            };

            if (Config.Item("coordb").GetValue<KeyBind>().Active)
            {
                bcoords = Game.CursorPos;
            };


            if (Config.Item("AutoDetonate", true).GetValue<bool>())
            {
                AutoExplode();

            };

        }
        public static void SetupMenu()
        {
            try
            {

                Config = new Menu("GangPlank", "GangPlank", true);

                var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
                TargetSelector.AddToMenu(targetSelectorMenu);
                Config.AddSubMenu(targetSelectorMenu);
                //Orbwalker submenu
                Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

                //Load the orbwalker and add it to the submenu.
                Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
                Config.AddSubMenu(new Menu("Barrel", "Barrel"));
                Config.SubMenu("Barrel").AddItem(new MenuItem("AutoDetonate", "Auto Detonate", true)).SetValue(true);
                Config.SubMenu("Barrel")
                    .AddItem(new MenuItem("detoneateTargets", "Blow up enemies with E"))
                    .SetValue(new Slider(2, 1, 5));
                Config.SubMenu("Barrel")
    .AddItem(new MenuItem("ScanIntense", "Increase if have lags"))
    .SetValue(new Slider(2, 1, 5));

                Config.AddSubMenu(new Menu("Combo", "Combo"));
                Config.SubMenu("Combo")
                    .AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

                Config.AddSubMenu(new Menu("Farm", "Farm"));
                Config.SubMenu("Farm").AddItem(new MenuItem("FarmActive", "Q farm", true)).SetValue(true);

                Config.AddSubMenu(new Menu("Debug", "Debug"));
                Config.SubMenu("Debug")
                                    .AddItem(new MenuItem("debugf", "Debug", true)).SetValue(true);
                Config.SubMenu("Debug")
                                    .AddItem(new MenuItem("trieplb", "Triple Barrel test", true)).SetValue(true);
                Config.SubMenu("Debug").AddItem(new MenuItem("coorda", "A!").SetValue(new KeyBind(32, KeyBindType.Press)));
                Config.SubMenu("Debug").AddItem(new MenuItem("coordb", "B!").SetValue(new KeyBind(32, KeyBindType.Press)));

                Config.AddToMainMenu();
                Console.WriteLine("menu initialize");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public static void Farm()
        {
   
            if (Q.IsReady() && Config.Item("FarmActive", true).GetValue<bool>())
            {

                var minion =
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(m => m.Health < Q.GetDamage(m) && m.SkinName != "GangplankBarrel" && HealthPrediction.GetHealthPrediction(m, (int)GetQTime(m)) > 0)
                        .OrderByDescending(m => m.MaxHealth)
                        .ThenByDescending(m => m.Distance(player))
                        .FirstOrDefault();

                if (minion != null)
                {
                    Q.CastOnUnit(minion);
                }

            }
        }
        public static void Drawing_OnDraw(EventArgs args)
        {
            if (acoords != null)
            {
                Render.Circle.DrawCircle(acoords, 60, System.Drawing.Color.Aqua);
            }
            if (bcoords != null)
            {
                Render.Circle.DrawCircle(bcoords, 60, System.Drawing.Color.Peru);
            }





            foreach (var barrels in savedbarrels)
            {
                if (barrels.IsValid)
                {
                    Render.Circle.DrawCircle(barrels.ServerPosition, BarrelExplosionRange, System.Drawing.Color.Aqua);
                }
            }
        }

        public static IEnumerable<Obj_AI_Minion> GetBarrels()
        {
            var MinionList =
                      ObjectManager.Get<Obj_AI_Minion>()
                          .Where(
                              minion =>
                                  minion.IsValidTarget(1500) && minion.Name == "Barrel" && minion.GetBuff("gangplankebarrellife").Caster.IsMe);
            return MinionList;
        }

        public static float getEActivationDelay()
        {
            if (player.Level >= 13)
            {
                return 0.5f;
            }
            if (player.Level >= 7)
            {
                return 1f;
            }
            return 2f;
        }
        public static float GetQTime(Obj_AI_Base targetB)
        {
            return player.Distance(targetB) / 2800f + 0.25f;
        }

        public static void DebugWrite(string text)
        {
            if (!Config.Item("debugf", true).GetValue<bool>())
                return;
            Console.WriteLine(text);
        }

        public static void AutoExplode()
        {
     
            foreach (var barrel in savedbarrels)
            {
                foreach (var enemy2 in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(Q.Range + BarrelExplosionRange) && hero.Distance(barrel) < BarrelExplosionRange-50))
                {
                    if (KillableBarrel(barrel, false) && Q.IsReady() && barrel.CountEnemiesInRange(BarrelExplosionRange) >= Config.Item("detoneateTargets").GetValue<Slider>().Value)
                    {
                        Q.Cast(barrel);
                    }
                }
            }
        }

        public static bool KillableBarrel(Obj_AI_Base targetB, bool ecastinclude)
        {
            float adddelay = 0;
            if (ecastinclude)
            {
                adddelay = 0.1f;
            }
            if (targetB.Health == 1)
            {
                return true;
            }
            var barrel = savedbarrels.FirstOrDefault(b => b.NetworkId == targetB.NetworkId);
            if (barrel != null)
            {

                var time = targetB.Health * getEActivationDelay();
                // DebugWrite(barrel.GetBuff("gangplankebarrellife").StartTime + " : " + Game.Time + " : " + GetQTime(targetB) + " : " + time);
                if (Game.Time - barrel.GetBuff("gangplankebarrellife").StartTime > time - GetQTime(targetB) - adddelay)
                {

                    return true;
                }
            }

            return false;
        }

        public static List<Vector3> PointsAroundTheTargetOuterRing(Vector3 pos, float dist, float width = 15)
        {
            List<Vector3> list = new List<Vector3>();
            int intensive = Config.Item("ScanIntense").GetValue<Slider>().Value;
            for (int a = 0; a < BarrelConnectionRange; a+=70*intensive)
                {
                    if (!pos.IsValid())
                    {
                        return new List<Vector3>();
                    }
                    
                    var max = 2 * a / 2 * Math.PI / width / 2;
                    var angle = 360f / max * Math.PI / 180.0f;
                    for (int i = 0; i < max; i++)
                    {
                        list.Add(
                            new Vector3(
                                pos.X + (float)(Math.Cos(angle * i) * a), pos.Y + (float)(Math.Sin(angle * i) * a),
                                pos.Z));
                    }
                }
            return list;
        }
        public static List<Vector3> GetBarrelPoints(Vector3 point)
        {
            return PointsAroundTheTargetOuterRing(point, BarrelConnectionRange, 20f);
        }

        public static int CheckRangeForBarrels(Vector3 position, int range)
        {
            return savedbarrels.Count(b => b.Distance(position) < range);
        }

        public static bool FindChainBarrels(Vector3 position)
        {
            Vector3 testposition = new Vector3(0, 0, 0);
            foreach (var barrel in savedbarrels)
            {
                if (barrel.Distance(position) < BarrelConnectionRange)
                {
                    if (barrel.CountEnemiesInRange(BarrelExplosionRange) >= Config.Item("detoneateTargets").GetValue<Slider>().Value)
                    {

                        return true;
                    }
                    else
                    {
                        testposition = barrel.ServerPosition;
                    }
                }
            }
            foreach (var barrel in savedbarrels)
            {
                if (barrel.Distance(testposition) < BarrelConnectionRange && barrel.CountEnemiesInRange(BarrelExplosionRange) >= Config.Item("detoneateTargets").GetValue<Slider>().Value)
                {
                    return true;
                }
            }
            return false;
        }



        public static void Combo()
        {
            try
            {
                var targetfore = TargetSelector.GetTarget(E.Range + BarrelExplosionRange, TargetSelector.DamageType.Physical);
                var targetforq = TargetSelector.GetTarget(Q.Range + BarrelExplosionRange, TargetSelector.DamageType.Physical);
                foreach (var barrel in savedbarrels.Where(b => b.IsValidTarget(Q.Range) && KillableBarrel(b, true)))
                {
                    var newP = GetBarrelPoints(barrel.Position).Where(p => !p.IsWall() && player.Distance(p)<E.Range+BarrelExplosionRange);
                    if (newP.Any())
                    {
                        barrelpoints.AddRange(newP.Where(p => p.Distance(player.Position) < E.Range));
                    }

                    foreach (var enemy1 in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(E.Range)))
                    {
                        barrelpoints.AddRange(newP.Where(p => p.Distance(player.Position) < E.Range));
                    }


                }
                if (barrelpoints.Any() && E.IsReady() && Q.IsReady() && targetfore != null)
                {
                    foreach (var secondbarrelpoint in barrelpoints)
                    {
                        if (secondbarrelpoint == null)
                        {
                            return;
                        }
                        // DebugWrite("finding second " + secondbarrelpoint.CountEnemiesInRange(BarrelExplosionRange));
                        if (secondbarrelpoint.CountEnemiesInRange(BarrelExplosionRange) >= Config.Item("detoneateTargets").GetValue<Slider>().Value)
                        {

                            var closest = barrelpoints.MinOrDefault(point => point.Distance(targetfore.ServerPosition));

                            if (closest != EDelay.position)
                            {
                                EDelay.position = closest;
                                EDelay.time = Environment.TickCount;
                                var qtarget = savedbarrels.MinOrDefault(b => b.IsValidTarget(Q.Range) && KillableBarrel(b, true) && b.Distance(closest) < BarrelConnectionRange);

                                E.Cast(closest);
                                Utility.DelayAction.Add(100, () => Q.Cast(qtarget));
                                if (Config.Item("trieplb", true).GetValue<bool>())
                                {
                                    foreach (var barrel in savedbarrels)
                                    {
                                        foreach (var enemy3 in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(E.Range + BarrelExplosionRange) && hero.Distance(barrel) > BarrelExplosionRange && hero.Distance(barrel) < BarrelConnectionRange))
                                        {
                                            Utility.DelayAction.Add(200, () => E.Cast(enemy3.ServerPosition));
                                        }
                                    }
                                }




                            }
                        }

                    }
                }
                var meleeRangeBarrel =
             savedbarrels.FirstOrDefault(
                 b =>
                     b.Health < 2 && KillableBarrel(b, true) &&
                     Orbwalking.InAutoAttackRange(b) &&
                     HeroManager.Enemies.Count(
                         o =>
                             o.IsValidTarget() && o.Distance(b) < BarrelExplosionRange &&
                             b.Distance(Prediction.GetPrediction(o, 500).UnitPosition) < BarrelExplosionRange) > 0);
                if (meleeRangeBarrel != null && !Q.IsReady())
                {
                    Orbwalker.SetAttack(false);
                    player.IssueOrder(GameObjectOrder.AttackUnit, meleeRangeBarrel);
                }
                if (Q.IsReady())
                {
                    foreach (var barrel in savedbarrels.Where(b => b.IsValidTarget(Q.Range) && KillableBarrel(b, false)))
                    {

                        if (FindChainBarrels(barrel.ServerPosition))
                        {
                            Q.Cast(barrel);
                        }

                    }

                }
                if (Q.IsReady())
                {
                    var barrelfound = false;
                    foreach (var barrel in savedbarrels.Where(b => b.IsValidTarget(Q.Range)))
                    {
                        if (barrel.Distance(targetforq) < BarrelExplosionRange)
                        {
                            barrelfound = true;
                            if (KillableBarrel(barrel, false))
                            {
                                Q.Cast(barrel);
                            }
                        }
                    }

                    if (!barrelfound || player.GetSpellDamage(targetforq, SpellSlot.Q) > targetforq.Health)
                    {
                        Q.Cast(targetforq);
                    }
                }
                barrelpoints = null;
                barrelpoints = new List<Vector3>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


     

    }
}