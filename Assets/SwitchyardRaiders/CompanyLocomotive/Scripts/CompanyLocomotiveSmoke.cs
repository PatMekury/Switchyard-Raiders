using UnityEngine;

namespace SwitchyardRaiders.CompanyTrain
{
    /// <summary>Small pooled plume; simulation can follow the movable tabletop,
    /// while emitted particles remain independent of the train's travel.</summary>
    [DisallowMultipleComponent]
    public sealed class CompanyLocomotiveSmoke : MonoBehaviour
    {
        public ParticleSystem chimneySmoke;
        public bool engineRunning = true;
        [Range(0,1)] public float engineLoad = .65f;
        [Tooltip("Optional tabletop/world root. Defaults to the locomotive's parent; leave empty for world space.")]
        public Transform simulationSpaceRoot;
        [Tooltip("Optional XR viewer. Defaults to Camera.main.")]
        public Transform viewer;
        private CompanyLocomotiveMotion motion;
        private Animator animator;
        private double previousDistance;
        private float chuffPhase;
        private bool distanceCulled;
        private static readonly int PlaybackSpeed = Animator.StringToHash("PlaybackSpeed");

        private void Awake() { Initialize(); }
        private void OnEnable() { if(chimneySmoke) chimneySmoke.Play(false); }
        private void OnDisable() { if(chimneySmoke) chimneySmoke.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear); }
        private void OnTransformParentChanged() { if(chimneySmoke) ConfigureSimulationSpace(); }

        public void Initialize()
        {
            motion=GetComponent<CompanyLocomotiveMotion>();
            animator=GetComponentInChildren<Animator>();
            if(!viewer && Camera.main) viewer=Camera.main.transform;
            if(motion) previousDistance=motion.TravelledMetres;
            ConfigureSimulationSpace();
        }

        public void ConfigureSimulationSpace()
        {
            if(!chimneySmoke)return;
            Transform space=simulationSpaceRoot?simulationSpaceRoot:transform.parent;
            var main=chimneySmoke.main;
            main.simulationSpace=space?ParticleSystemSimulationSpace.Custom:ParticleSystemSimulationSpace.World;
            main.customSimulationSpace=space;
        }

        public void SetEngineRunning(bool running) { engineRunning=running; }

        private void LateUpdate()
        {
            if(!chimneySmoke)return;
            float speed=0;
            if(motion && motion.motionDriver==CompanyLocomotiveMotion.MotionDriver.RailDistance)
            {
                if(Time.deltaTime>0)speed=(float)System.Math.Abs(motion.TravelledMetres-previousDistance)/Time.deltaTime;
                previousDistance=motion.TravelledMetres;
            }
            else if(animator && animator.enabled)
            {
                var state=animator.GetCurrentAnimatorStateInfo(0);
                speed=state.IsName("Base Layer.Reverse")?2.591814f:2.591814f*Mathf.Abs(animator.GetFloat(PlaybackSpeed));
            }
            // Distances in authored locomotive metres, so miniature and life-size
            // presentations get comparable screen-size reduction.
            float distance=viewer?Vector3.Distance(viewer.position,chimneySmoke.transform.position)/Mathf.Max(.001f,Mathf.Abs(transform.lossyScale.x)):0;
            UpdateEmission(speed,distance,Time.deltaTime);
        }

        public void UpdateEmission(float metresPerSecond,float distanceInAssetMetres,float deltaTime)
        {
            if(!chimneySmoke)return;
            bool cull=distanceInAssetMetres>=90;
            if(cull)
            {
                if(!distanceCulled)chimneySmoke.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                distanceCulled=true;return;
            }
            if(distanceCulled){chimneySmoke.Play(false);distanceCulled=false;}
            float speed=Mathf.Clamp(Mathf.Abs(metresPerSecond),0,8);
            chuffPhase=Mathf.Repeat(chuffPhase+speed/(2*Mathf.PI*.825f)*4*Mathf.Max(0,deltaTime),1);
            float pulse=.85f+.30f*Mathf.Pow(.5f+.5f*Mathf.Cos(chuffPhase*2*Mathf.PI),2);
            float distanceFade=1-Mathf.InverseLerp(45,90,distanceInAssetMetres);
            float load=Mathf.Clamp01(engineLoad);
            var emission=chimneySmoke.emission;
            emission.rateOverTime=engineRunning?Mathf.Lerp(3,15,Mathf.Clamp01(speed/2.6f))*Mathf.Lerp(.55f,1,load)*pulse*distanceFade:0;
            var main=chimneySmoke.main;
            main.startSpeed=new ParticleSystem.MinMaxCurve(Mathf.Lerp(.65f,1.15f,load),Mathf.Lerp(1.05f,1.75f,load));
        }
    }
}
