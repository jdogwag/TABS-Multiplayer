﻿using System;
using TABS_Multiplayer;
using UnityEngine;

#pragma warning disable CS0626
namespace Landfall.TABS.UnitPlacement
{
    class patch_UnitBrush : UnitBrush
    {
        private BrushBehaviourBase m_brushBehaviour; // Expose private fields to this patch class
        private PlacementCursor m_cursor;
        private PlacementCursorVisuals m_cursorVisuals;

        public extern void orig_InitializeBrushBehaviour(GameModeState state);
        public new void InitializeBrushBehaviour(GameModeState state)
        {
            orig_InitializeBrushBehaviour(state);
            if(state == GameModeState.Sandbox)
            {
                m_brushBehaviour = new BrushBehaviorMultiplayer(); // Set the brush behavior to our own one
                m_brushBehaviour.SetBrushCursor(m_cursor, m_cursorVisuals);
            }
        }

        public extern void orig_PlaceUnit(UnitBlueprint blueprint, Team team, Vector3 position, bool costsBudget);
        public new void PlaceUnit(UnitBlueprint blueprint, Team team, Vector3 position, bool costsBudget = true)
        {
            orig_PlaceUnit(blueprint, team, position, costsBudget);
            if (costsBudget) // Don't send the unit when it's placed by the resetter
            {
                SocketConnection.SetCulture();
                SocketConnection.WriteToOpponent("SPAWNUNIT|" + blueprint.Entity.ID + "|" + team.ToString() +
                    "|" + position.ToString("F5"));
                // Send a spawn command
            }
        }

        public extern void orig_RemoveUnit(Unit unit, Team team);
        public new void RemoveUnit(Unit unit, Team team)
        {
            orig_RemoveUnit(unit, team);
            SocketConnection.SetCulture();
            SocketConnection.WriteToOpponent("REMOVEUNIT|" + unit.transform.position.ToString("F5") + "|" + team.ToString());
            // Send a remove command with its position (it acts like an inaccurate GUID)
        }

        public BrushBehaviourBase GetBrushBehaviour()
        {
            return m_brushBehaviour;
        }


    }

    class BrushBehaviorMultiplayer : BrushBehaviourSandbox
    {
        public override bool CanRemove(Unit unit)
        {
            bool isServer = SocketConnection.GetIsServer();
            return ((isServer && !SocketConnection.getTcpClient().Connected) 
                || (unit.Team == (isServer ? Team.Red : Team.Blue)) && !SocketConnection.gameStarted);
            // Make sure that each client can only remove on their position if it's connected
        }

        public override void CursorUpdate(Vector3 position)
        {
            base.CursorUpdate(position);
            bool isServer = SocketConnection.GetIsServer();
            if(!(isServer && !SocketConnection.getTcpClient().Connected) 
                && GetTeamAreaAtPosition(position) != (isServer ? Team.Red : Team.Blue))
            {
                base.PlacementCursor.Renderer.material = base.PlacementCursorVisuals.DenyPlaceMaterial;
            }
            // Show a block icon if the cursor hovers over the opponent if it's connected
        }

        public override bool CanPlace(UnitBlueprint unitToSpawn, Team team, ref Vector3 position)
        {
            bool isServer = SocketConnection.GetIsServer();
            return (base.CanPlace(unitToSpawn, team, ref position) 
                && ((isServer && !SocketConnection.getTcpClient().Connected) ? true :
                (GetTeamAreaAtPosition(position) == (isServer ? Team.Red : Team.Blue) && !SocketConnection.gameStarted)));
            // Make sure that each client can only place on their position if it's connected
        }
    }
}
