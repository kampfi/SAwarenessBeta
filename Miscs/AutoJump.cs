﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAssemblies.Miscs
{
    class AutoJump
    {
        public static Menu.MenuItemSettings AutoJumpMisc = new Menu.MenuItemSettings(typeof(AutoJump));

        private Spell _jumpSpell;
        private readonly bool _onlyAlly;
        private readonly bool _onlyEnemy;
        private readonly bool _useWard = true;
        private float _lastCast = Game.Time;
        private int lastGameUpdateTime = 0;

        public AutoJump()
        {
            switch (ObjectManager.Player.ChampionName)
            {
                case "Katarina":
                    _jumpSpell = new Spell(SpellSlot.E, 790);
                    break;

                case "Jax":
                    _jumpSpell = new Spell(SpellSlot.Q, 790);
                    break;

                case "LeeSin":
                    _jumpSpell = new Spell(SpellSlot.W, 790);
                    _onlyAlly = true;
                    break;

                case "Talon":
                    _jumpSpell = new Spell(SpellSlot.E, 790);
                    _onlyEnemy = true;
                    _useWard = false;
                    break;

                default:
                    return;
            }
            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
        }

        ~AutoJump()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
            GameObject.OnCreate -= Obj_AI_Base_OnCreate;
            _jumpSpell = null;
            _lastCast = 0;
        }

        public bool IsActive()
        {
#if MISCS
            return Misc.Miscs.GetActive() && AutoJumpMisc.GetActive();
#else
            return AutoJumpMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            AutoJumpMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_AUTOJUMP_MAIN"), "SAssembliesMiscsAutoJump"));
            AutoJumpMisc.MenuItems.Add(
                AutoJumpMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsAutoJumpKey", Language.GetString("GLOBAL_KEY")).SetValue(new KeyBind(85, KeyBindType.Press))));
            AutoJumpMisc.MenuItems.Add(
                AutoJumpMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsAutoJumpActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(false)));
            return AutoJumpMisc;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || !AutoJumpMisc.GetMenuItem("SAssembliesMiscsAutoJumpKey").GetValue<KeyBind>().Active ||
                !_jumpSpell.IsReady() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            lastGameUpdateTime = Environment.TickCount;

            foreach (GameObject gObject in ObjectManager.Get<GameObject>())
            {
                if ((_useWard && (gObject.Name.Contains("SightWard") || gObject.Name.Contains("VisionWard"))) ||
                    gObject.Type == GameObjectType.obj_AI_Minion || gObject.Type == GameObjectType.obj_AI_Hero)
                {
                    if (!_onlyAlly && !_onlyEnemy || (_onlyAlly && gObject.IsAlly) || (_onlyEnemy && gObject.IsEnemy))
                    {
                        if (!gObject.IsValid || ((Obj_AI_Base)gObject).Health < 1)
                            continue;
                        if (Game.CursorPos.Distance(gObject.Position) > 150)
                            continue;
                        if (_lastCast + 1 > Game.Time)
                            continue;
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, gObject);
                        _jumpSpell.Cast((Obj_AI_Base)gObject, true);
                        _lastCast = Game.Time;
                        return;
                    }
                }
            }
            if (_jumpSpell.IsReady() && _useWard)
            {
                if (_lastCast + 1 > Game.Time)
                    return;
                InventorySlot slot = Ward.GetWardSlot();
                ObjectManager.Player.Spellbook.CastSpell(slot.SpellSlot, Game.CursorPos);
                _jumpSpell.Cast(Game.CursorPos, true);
                _lastCast = Game.Time;
            }
        }

        private void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (!IsActive() || !AutoJumpMisc.GetMenuItem("SAssembliesMiscsAutoJumpKey").GetValue<KeyBind>().Active ||
                !_jumpSpell.IsReady())
                return;
            if (sender.Name.Contains("SightWard") || sender.Name.Contains("VisionWard"))
            {
                if (Game.CursorPos.Distance(sender.Position) > 150)
                    return;
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, sender);
                _jumpSpell.Cast((Obj_AI_Base)sender, true);
                _lastCast = Game.Time;
            }
        }
    }
}
