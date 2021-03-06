﻿using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework.Packaging;

namespace MissingSerializersFix
{
    public class PackageHelperDetour
    {
        public const string STUB_TREE = "tree01";
        public const string STUB_PROP = "Door Marker";
        public const string STUB_SUB_BULDING = "Metro Entrance";
        private static RedirectCallsState state;
        private static IntPtr _intPtr;
        private static IntPtr _intPtr2;

        private static RedirectCallsState state2;
        private static IntPtr _intPtr1;
        private static IntPtr _intPtr3;

        public static readonly Dictionary<BuildingInfo.SubInfo, string> SubBuildings = new Dictionary<BuildingInfo.SubInfo, string>();
        public static readonly Dictionary<string, List<string>> PropVariations = new Dictionary<string, List<string>>();
        public static readonly Dictionary<TreeInfo.Variation, string> TreeVariations = new Dictionary<TreeInfo.Variation, string>();

        public static void Init()
        {
            _intPtr = typeof(PackageHelper).GetMethod("CustomSerialize",
                BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer();
            _intPtr2 = typeof(PackageHelperDetour).GetMethod("CustomSerialize",
                BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer();
            state = RedirectionHelper.PatchJumpTo(
                _intPtr,
                _intPtr2
                );
            _intPtr1 = typeof(PackageHelper).GetMethod("CustomDeserialize",
                BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer();
            _intPtr3 = typeof(PackageHelperDetour).GetMethod("CustomDeserialize",
                BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer();
            state2 = RedirectionHelper.PatchJumpTo(
                _intPtr1,
                _intPtr3
                );
        }

        public static void Revert()
        {
            RedirectionHelper.RevertJumpTo(_intPtr, state);
            RedirectionHelper.RevertJumpTo(_intPtr1, state2);
        }

        public static bool CustomSerialize(Package p, object o, PackageWriter w)
        {
            if (o.GetType() == typeof(PropInfo.Variation))
            {
                PropInfo.Variation varitation = (PropInfo.Variation)o;
                w.Write(varitation.m_prop.name);
                w.Write(varitation.m_probability);
                return true;
            }
            if (o.GetType() == typeof(TreeInfo.Variation))
            {
                TreeInfo.Variation varitation = (TreeInfo.Variation)o;
                w.Write(varitation.m_tree.name);
                w.Write(varitation.m_probability);
                return true;
            }
            RedirectionHelper.RevertJumpTo(_intPtr, state);
            try
            {
                return PackageHelper.CustomSerialize(p, o, w);
            }
            finally
            {
                RedirectionHelper.PatchJumpTo(_intPtr, _intPtr2);
            }

        }

        public static object CustomDeserialize(Package p, Type t, PackageReader r)
        {
            if (t == typeof(BuildingInfo.SubInfo))
            {
                var stubSubBuilding = PrefabCollection<BuildingInfo>.FindLoaded(STUB_SUB_BULDING);

                BuildingInfo.SubInfo subInfo = new BuildingInfo.SubInfo();
                var buildingId = r.ReadString();
                subInfo.m_buildingInfo = stubSubBuilding;
                subInfo.m_position = r.ReadVector3();
                subInfo.m_angle = r.ReadSingle();
                subInfo.m_fixedHeight = r.ReadBoolean();

                SubBuildings.Add(subInfo, buildingId);
                return (object)subInfo;
            }
            if (t == typeof(PropInfo.Variation))
            {
                var propId = r.ReadString();
                var mainPropId = $"{p.packageName}.{p.packageMainAsset}_Data";
                if (!PropVariations.ContainsKey(mainPropId))
                {
                    PropVariations.Add(mainPropId, new List<string>());
                }
                PropVariations[mainPropId].Add(propId);
                var stubProp = PrefabCollection<PropInfo>.FindLoaded(STUB_PROP); //a fake prop to prevent exception
                return new PropInfo.Variation()
                {
                    m_prop = stubProp,
                    m_finalProp = stubProp,
                    m_probability = r.ReadInt32()
                };
            }
            if (t == typeof(TreeInfo.Variation))
            {
                var stubTree = PrefabCollection<TreeInfo>.FindLoaded(STUB_TREE); //a fake tree to prevent exception

                var variation = new TreeInfo.Variation();
                var treeId = r.ReadString();
                variation.m_tree = stubTree;
                variation.m_finalTree = stubTree;
                variation.m_probability = r.ReadInt32();
                TreeVariations.Add(variation, treeId);
                return variation;
            }
            RedirectionHelper.RevertJumpTo(_intPtr1, state2);
            try
            {
                return PackageHelper.CustomDeserialize(p, t, r);
            }
            finally
            {
                RedirectionHelper.PatchJumpTo(_intPtr1, _intPtr3);
            }
        }
    }
}