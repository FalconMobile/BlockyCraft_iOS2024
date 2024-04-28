using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public delegate VoxelDefinition CustomVoxelDefinitionProviderDelegate (Vector3d position, VoxelDefinition vd);

    public enum ConnectedVoxelConfigMatch
    {
        Anything,
        Equals,
        NotEquals,
        Empty,
        NotEmpty
    }

    public enum ConnectedVoxelConfigAction
    {
        Nothing,
        Replace,
        Random,
        Cycle
    }


    [Serializable]
    public struct ConnectedVoxelConfig
    {
        public bool enabled;
        public ConnectedVoxelConfigMatch tl, t, tr, l, r, bl, b, br;
        public ConnectedVoxelConfigAction action;
        public VoxelDefinition replacementVoxelDefinition; // for Replace action
        public VoxelDefinition [] replacementVoxelDefinitionSet; // for Random/Cycle action
    }

    [CreateAssetMenu (menuName = "Voxel Play/Connected Voxel", fileName = "ConnectedVoxel", order = 132)]
    public class ConnectedVoxel : ScriptableObject
    {

        public string description;

        [Tooltip ("The voxel the user is placing.")]
        public VoxelDefinition voxelDefinition;

        [Tooltip ("Rules that apply to this voxel.")]
        public ConnectedVoxelConfig [] config;

        VoxelPlayEnvironment env;
        int cycleIndex;
        VoxelIndex [] voxelIndices;

        public void Init (VoxelPlayEnvironment env)
        {
            this.env = env;
            if (voxelDefinition == null || config == null) return;

            voxelIndices = new VoxelIndex [9];
            voxelDefinition.customVoxelDefinitionProvider = ResolveVoxelDefinition;
        }


        public VoxelDefinition ResolveVoxelDefinition (Vector3d position, VoxelDefinition vd)
        {
            if (config == null || env == null)
                return vd;

            env.GetVoxelNeighbourhood (position, ref voxelIndices);
            int forwardLeftTypeIndex = voxelIndices [6].typeIndex;
            int forwardTypeIndex = voxelIndices [7].typeIndex;
            int forwardRightTypeIndex = voxelIndices [8].typeIndex;
            int leftTypeIndex = voxelIndices [3].typeIndex;
            int rightTypeIndex = voxelIndices [5].typeIndex;
            int backLeftTypeIndex = voxelIndices [0].typeIndex;
            int backTypeIndex = voxelIndices [1].typeIndex;
            int backRightTypeIndex = voxelIndices [2].typeIndex;
            for (int k = 0; k < config.Length; k++) {
                bool pass = CheckConfigRuleMatch (config [k].tl, forwardLeftTypeIndex);
                if (pass) pass = CheckConfigRuleMatch (config [k].t, forwardTypeIndex);
                if (pass) pass = CheckConfigRuleMatch (config [k].tr, forwardRightTypeIndex);
                if (pass) pass = CheckConfigRuleMatch (config [k].l, leftTypeIndex);
                if (pass) pass = CheckConfigRuleMatch (config [k].r, rightTypeIndex);
                if (pass) pass = CheckConfigRuleMatch (config [k].bl, backLeftTypeIndex);
                if (pass) pass = CheckConfigRuleMatch (config [k].b, backTypeIndex);
                if (pass) pass = CheckConfigRuleMatch (config [k].br, backRightTypeIndex);
                if (!pass) continue;

                switch (config [k].action) {
                case ConnectedVoxelConfigAction.Nothing:
                    vd = null;
                    break;
                case ConnectedVoxelConfigAction.Replace:
                    vd = config [k].replacementVoxelDefinition;
                    break;
                case ConnectedVoxelConfigAction.Random: {
                        VoxelDefinition [] replacementSet = config [k].replacementVoxelDefinitionSet;
                        if (replacementSet != null && replacementSet.Length > 0) {
                            int index = WorldRand.Range (0, replacementSet.Length, position);
                            vd = replacementSet [index];
                        }
                    }
                    break;
                case ConnectedVoxelConfigAction.Cycle: {
                        VoxelDefinition [] replacementSet = config [k].replacementVoxelDefinitionSet;
                        if (replacementSet != null && replacementSet.Length > 0) {
                            cycleIndex++;
                            if (cycleIndex >= replacementSet.Length) {
                                cycleIndex = 0;
                            }
                            vd = replacementSet [cycleIndex];
                        }
                    }
                    break;
                }
                break;
            }
            return vd; // rule executed, exit
        }


        bool CheckConfigRuleMatch (ConnectedVoxelConfigMatch match, int typeIndex)
        {
            switch (match) {
            case ConnectedVoxelConfigMatch.Empty: return typeIndex == 0;
            case ConnectedVoxelConfigMatch.NotEmpty: return typeIndex > 0;
            case ConnectedVoxelConfigMatch.Equals: return voxelDefinition == null || voxelDefinition.index == typeIndex;
            case ConnectedVoxelConfigMatch.NotEquals: return voxelDefinition == null || voxelDefinition.index != typeIndex;
            default: return true;
            }
        }

    }

    public partial class VoxelDefinition : ScriptableObject
    {
        [NonSerialized]
        public CustomVoxelDefinitionProviderDelegate customVoxelDefinitionProvider;
    }

}
