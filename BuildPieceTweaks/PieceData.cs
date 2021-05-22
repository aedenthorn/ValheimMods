using System.Collections.Generic;
using UnityEngine;

namespace BuildPieceTweaks
{
    internal class PieceData
    {
        public string name;

        public Piece.PieceCategory category;
        public Piece.ComfortGroup comfortGroup;
        public int comfort;
        public bool groundPiece;
        public bool allowAltGroundPlacement;
        public bool groundOnly;
        public bool cultivatedGroundOnly;
        public bool waterPiece;
        public bool clipGround;
        public bool clipEverything;
        public bool noInWater;
        public bool notOnWood;
        public bool notOnTiltingSurface;
        public bool inCeilingOnly;
        public bool notOnFloor;
        public bool noClipping;
        public bool onlyInTeleportArea;
        public bool allowedInDungeons;
        public float spaceRequirement;
        public bool repairPiece;
        public bool canBeRemoved;
        public string station = "";
        public Heightmap.Biome onlyInBiome;

        public float health;
        public bool noRoofWear;
        public bool noSupportWear;
        public WearNTear.MaterialType materialType;
        public bool supports;
        public Vector3 comOffset = Vector3.zero;
        public float hitNoise;
        public float destroyNoise;
        public bool autoCreateFragments;
        public List<string> damageModifiers = new List<string>();
	}
}