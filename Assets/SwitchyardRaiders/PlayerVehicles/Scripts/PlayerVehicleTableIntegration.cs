using UnityEngine;
using SwitchyardRaiders.Startup;
namespace SwitchyardRaiders.PlayerVehicles
{
    /// <summary>Replaces the two player placeholders at runtime, retaining the authored scene.
    /// Company locomotive and cargo are untouched; RaidersTable continues to own root travel.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerVehicleTableIntegration : MonoBehaviour
    {
        public RaidersTable table;
        public GameObject[] playerPrefabs = new GameObject[2];
        [Tooltip("Real metres to existing tabletop units. A 14.8m train becomes 5.92cm.")]
        public float modelScale=.004f;
        private bool installed;
        private void Start(){Install();}
        public void Install()
        {
            if(installed||!table||table.locomotives==null||table.locomotives.Length<2)return;
            if(playerPrefabs==null||playerPrefabs.Length<2||!playerPrefabs[0]||!playerPrefabs[1])return;
            for(int i=0;i<2;i++)
            {
                var old=table.locomotives[i];if(!old)continue;
                var go=Instantiate(playerPrefabs[i],old.parent);go.name="Player "+(i+1)+" battery locomotive";
                go.transform.SetLocalPositionAndRotation(old.localPosition+Vector3.up*.003f,old.localRotation);
                go.transform.localScale=Vector3.one*modelScale;
                var follower=go.AddComponent<PlayerVehicleTrackFollower>();follower.table=table;follower.playerSlot=i;
                old.gameObject.SetActive(false);table.locomotives[i]=go.transform;
            }
            installed=true;
        }
    }
}
