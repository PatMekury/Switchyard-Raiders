/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Oculus.Avatar2;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using static Oculus.Interaction.Input.HandMirroring;

namespace SwitchyardRaiders.Startup
{
    // SDK 40 provider registration follows Meta's ISDK Avatar Integration sample.
    // World-space ISDK data is converted into the existing physical tracking space once.
    // No rig, synthetic hand, palm classifier or interactor is moved or disabled.
    public sealed class RaidersAvatarInput : OvrAvatarInputManager, IOvrAvatarInputTrackingDelegate, IOvrAvatarHandTrackingDelegate
    {
        private IHand left, right;
        private IHmd hmd;
        private Transform space;
        public void Configure(IHand leftHand,IHand rightHand,IHmd head,Transform trackingSpace)
        {
            left=leftHand;right=rightHand;hmd=head;space=trackingSpace;
            _bodyTrackingMode=OvrAvatarBodyTrackingMode.None;
            InstallProviders();
        }
        private void InstallProviders()
        {
            _inputTrackingProvider=new OvrAvatarInputTrackingDelegatedProvider(this);
            _handTrackingProvider=new OvrAvatarHandTrackingDelegatedProvider(this);
        }
        protected override void OnTrackingInitialized(){base.OnTrackingInitialized();InstallProviders();}
        public static bool Valid(IHand hand)=>hand!=null && hand.IsConnected && hand.IsTrackedDataValid && hand.IsHighConfidence;
        public bool TrackingReady=>Valid(left)&&Valid(right)&&left.GetRootPose(out _)&&right.GetRootPose(out _)&&left.GetJointPosesLocal(out _)&&right.GetJointPosesLocal(out _)&&hmd!=null&&hmd.TryGetRootPose(out _);
        public static Pose InTrackingSpace(Transform trackingSpace,Pose world)=>new Pose(
            trackingSpace.InverseTransformPoint(world.position),Quaternion.Inverse(trackingSpace.rotation)*world.rotation);
        public bool GetInputTrackingState(out OvrAvatarInputTrackingState state)
        {
            state=default;if(!space || hmd==null)return false;
            if(hmd.TryGetRootPose(out var head)){state.headsetActive=true;state.headset=PoseValue(InTrackingSpace(space,head));}
            if(Valid(left)&&left.GetRootPose(out var lp)){state.leftControllerActive=true;state.leftController=PoseValue(InTrackingSpace(space,lp));}
            if(Valid(right)&&right.GetRootPose(out var rp)){state.rightControllerActive=true;state.rightController=PoseValue(InTrackingSpace(space,rp));}
            return state.headsetActive;
        }
        public bool GetHandData(OvrAvatarTrackingHandsState data)
        {
            if(!space || left==null || right==null)return false;
            data.isTrackedLeft=data.isConfidentLeft=Valid(left);
            data.isTrackedRight=data.isConfidentRight=Valid(right);
            if(data.isTrackedLeft && left.GetRootPose(out var lp))data.wristPosLeft=WristFor(left,InTrackingSpace(space,lp));
            else data.isTrackedLeft=data.isConfidentLeft=false;
            if(data.isTrackedRight && right.GetRootPose(out var rp))data.wristPosRight=WristFor(right,InTrackingSpace(space,rp));
            else data.isTrackedRight=data.isConfidentRight=false;
            if(data.isTrackedLeft && !CopyJoints(left,data.boneRotations,0))data.isTrackedLeft=data.isConfidentLeft=false;
            if(data.isTrackedRight && !CopyJoints(right,data.boneRotations,17))data.isTrackedRight=data.isConfidentRight=false;
            data.handScaleLeft=left.Scale;data.handScaleRight=right.Scale;
            return true;
        }
        // Installed ISDK 207 exports OpenXR joints (its assembly-local ISDK_OPENXR_HAND).
        // Application assemblies do not inherit package version defines.
        // Coordinate conversions / OpenXR-to-avatar 17-joint mapping adapted from Meta's official sample:
        // github.com/oculus-samples/Unity-MetaXRInteractionSDK-AvatarSample (Oculus SDK License).
        public static CAPI.ovrAvatar2Transform PoseValue(Pose p)=>new CAPI.ovrAvatar2Transform(p.position,p.rotation);
        public static CAPI.ovrAvatar2Transform WristValue(Pose p)=>new CAPI.ovrAvatar2Transform(
            new CAPI.ovrAvatar2Vector3f{x=p.position.x,y=p.position.y,z=-p.position.z},
            new CAPI.ovrAvatar2Quatf{x=-p.rotation.x,y=-p.rotation.y,z=p.rotation.z,w=p.rotation.w});

        private static readonly int[] _sourceJoints = new int[]
        {
             (int) HandJointId.Invalid,
             (int) HandJointId.HandThumb1,
             (int) HandJointId.HandThumb2,
             (int) HandJointId.HandThumb3,
             (int) HandJointId.HandIndex1,
             (int) HandJointId.HandIndex2,
             (int) HandJointId.HandIndex3,
             (int) HandJointId.HandMiddle1,
             (int) HandJointId.HandMiddle2,
             (int) HandJointId.HandMiddle3,
             (int) HandJointId.HandRing1,
             (int) HandJointId.HandRing2,
             (int) HandJointId.HandRing3,
             (int) HandJointId.HandPinky0,
             (int) HandJointId.HandPinky1,
             (int) HandJointId.HandPinky2,
             (int) HandJointId.HandPinky3,
        };

        private static readonly HandsSpace _inputHands = HandTranslationUtils.openXRHands;

        private static readonly HandSpace _avatarHandSpace = new HandSpace(
            Vector3.left, Vector3.up, Vector3.forward);
        private static readonly HandSpace _avatarRootLeft = new HandSpace(
            Vector3.back, Vector3.up, Vector3.right);
        private static readonly HandSpace _avatarRootRight = new HandSpace(
            Vector3.back, Vector3.up, Vector3.left);
        private static Quaternion _leftRootOffset = Quaternion.Euler(0f, -90f, 180f);
        private static Quaternion _rightRootOffset = Quaternion.Euler(0f, -90f, 0f);

        private static readonly HandsSpace _avatarRootHands = new HandsSpace(_avatarRootLeft, _avatarRootRight);

        private static readonly int JOINTS_PER_HAND = _sourceJoints.Length;

        private static CAPI.ovrAvatar2Transform WristFor(IHand hand,Pose pose)
        {
            pose=TransformPose(pose,_inputHands[hand.Handedness],_avatarRootHands[hand.Handedness]);
            pose.rotation=pose.rotation*(hand.Handedness==Handedness.Left?_leftRootOffset:_rightRootOffset);
            return new CAPI.ovrAvatar2Transform(pose.position,pose.rotation);
        }
        private static bool CopyJoints(IHand hand,CAPI.ovrAvatar2Quatf[] destination,int offset)
        {
            if(!hand.GetJointPosesLocal(out var poses) || poses==null || poses.Count<=(int)HandJointId.HandPinky3)return false;
            for(int i=0;i<JOINTS_PER_HAND;i++)
            {
                int index=_sourceJoints[i];
                destination[offset+i]=index<0?Quaternion.identity:TransformRotation(poses[index].rotation,_inputHands[hand.Handedness],_avatarHandSpace);
            }
            return true;
        }

    }
}
