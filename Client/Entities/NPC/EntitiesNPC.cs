﻿using System.Collections.Generic;

using GTA;
using GTA.Native;
using GTA.Math;

namespace CoopClient.Entities.NPC
{
    internal partial class EntitiesNPC
    {
        internal bool LastSyncWasFull { get; set; } = false;
        public ulong LastUpdateReceived { get; set; }

        internal Ped Character { get; set; }
        internal int Health { get; set; }
        private int LastModelHash = 0;
        private int CurrentModelHash = 0;

        internal int ModelHash
        {
            get => CurrentModelHash;
            set
            {
                LastModelHash = LastModelHash == 0 ? value : CurrentModelHash;
                CurrentModelHash = value;
            }
        }
        private Dictionary<byte, short> LastClothes = null;
        internal Dictionary<byte, short> Clothes { get; set; }

        internal Vector3 Position { get; set; }
        internal Vector3 AimCoords { get; set; }

        internal void DisplayLocally()
        {
            #region NOT_IN_RANGE
            if (!Game.Player.Character.IsInRange(Position, 500f))
            {
                if (Character != null && Character.Exists())
                {
                    Character.Kill();
                    Character.MarkAsNoLongerNeeded();
                    Character.Delete();
                    Character = null;
                }

                return;
            }
            #endregion

            #region IS_IN_RANGE
            bool characterExist = Character != null && Character.Exists();

            if (!characterExist)
            {
                if (!CreateCharacter())
                {
                    return;
                }
            }
            else if (LastSyncWasFull)
            {
                if (CurrentModelHash != LastModelHash)
                {
                    Character.Kill();
                    Character.Delete();

                    if (!CreateCharacter())
                    {
                        return;
                    }
                }
                else if (!Clothes.Compare(LastClothes))
                {
                    foreach (KeyValuePair<byte, short> cloth in Clothes)
                    {
                        Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character.Handle, cloth.Key, cloth.Value, 0, 0);
                    }

                    LastClothes = Clothes;
                }
            }

            if (Character.IsDead)
            {
                if (Health <= 0)
                {
                    return;
                }

                Character.IsInvincible = true;
                Character.Resurrect();
            }
            else if (Character.Health != Health)
            {
                Character.Health = Health;

                if (Health <= 0 && !Character.IsDead)
                {
                    Character.IsInvincible = false;
                    Character.Kill();
                    return;
                }
            }

            if (IsInVehicle)
            {
                DisplayInVehicle();
            }
            else
            {
                DisplayOnFoot();
            }
            #endregion
        }

        private bool CreateCharacter()
        {
            Model characterModel = CurrentModelHash.ModelRequest();

            if (characterModel == null)
            {
                //GTA.UI.Notification.Show($"~r~(Character)Model ({CurrentModelHash}) cannot be loaded!");
                return false;
            }

            Character = World.CreatePed(characterModel, Position, Rotation.Z);
            characterModel.MarkAsNoLongerNeeded();

            Character.RelationshipGroup = Main.RelationshipGroup;
            Character.Health = Health;
            if (IsInVehicle)
            {
                Character.IsVisible = false;
            }
            Character.BlockPermanentEvents = true;
            Character.CanRagdoll = false;
            Character.IsInvincible = true;

            Character.CanSufferCriticalHits = false;

            Function.Call(Hash.SET_PED_CAN_EVASIVE_DIVE, Character.Handle, false);
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED, Character.Handle, true);
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED_BY_PLAYER, Character.Handle, Game.Player, true);
            Function.Call(Hash.SET_PED_GET_OUT_UPSIDE_DOWN_VEHICLE, Character.Handle, false);
            Function.Call(Hash.SET_PED_AS_ENEMY, Character.Handle, false);
            Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, Character.Handle, true, true);

            foreach (KeyValuePair<byte, short> cloth in Clothes)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character.Handle, cloth.Key, cloth.Value, 0, 0);
            }

            return true;
        }
    }
}
