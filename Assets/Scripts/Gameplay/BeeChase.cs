using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace HackathonVR.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class BeeChase : MonoBehaviour
    {
        public Transform player;
        public float detectionRange = 10f;
        public float catchDistance = 1.5f;
        public float patrolRadius = 15f;
        
        private NavMeshAgent agent;
        private Vector3 startPos;
        private static bool isPlayerHidden = false;
        
        private enum State { Patrol, Chase, Return }
        private State currentState = State.Patrol;

        public static void SetPlayerHidden(bool hidden)
        {
            isPlayerHidden = hidden;
        }

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            startPos = transform.position;
            
            if (player == null)
            {
                var rig = GameObject.Find("XR Origin (XR Rig)");
                if (rig == null) rig = GameObject.Find("VR Setup");
                if (rig != null) player = rig.transform;
            }
            
            // Random patrol start
            PickRandomPatrolPoint();
        }

        private void Update()
        {
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            switch (currentState)
            {
                case State.Patrol:
                    if (!isPlayerHidden && distanceToPlayer < detectionRange)
                    {
                        currentState = State.Chase;
                        // Play sound?
                    }
                    
                    if (!agent.pathPending && agent.remainingDistance < 0.5f)
                    {
                        PickRandomPatrolPoint();
                    }
                    break;

                case State.Chase:
                    if (isPlayerHidden || distanceToPlayer > detectionRange * 1.5f)
                    {
                        currentState = State.Return;
                        agent.SetDestination(startPos);
                    }
                    else
                    {
                        agent.SetDestination(player.position);
                        
                        if (distanceToPlayer < catchDistance)
                        {
                            CatchPlayer();
                        }
                    }
                    break;

                case State.Return:
                    if (!isPlayerHidden && distanceToPlayer < detectionRange)
                    {
                        currentState = State.Chase;
                    }
                    
                    if (!agent.pathPending && agent.remainingDistance < 0.5f)
                    {
                        currentState = State.Patrol;
                    }
                    break;
            }
        }

        private void PickRandomPatrolPoint()
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir += startPos;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, 1))
            {
                agent.SetDestination(hit.position);
            }
        }

        private void CatchPlayer()
        {
            Debug.Log("Player Caught!");
            // Respawn logic
            var gm = Core.SceneSpawnManager.Instance;
            if (gm != null)
            {
                // Reload current scene or respawn
                // Simple respawn:
                if (Core.DialogueManager.Instance != null)
                    Core.DialogueManager.Instance.ShowMessage("GAME OVER", "Les abeilles vous ont attrapé !", 3f);
                    
                // Teleport back to start of scene 3
                var spawnData = gm.spawnDataList.Find(x => x.sceneName == "3");
                if (spawnData != null && player != null)
                {
                   player.position = spawnData.position;
                }
            }
            currentState = State.Return;
            agent.SetDestination(startPos);
        }
    }
}
