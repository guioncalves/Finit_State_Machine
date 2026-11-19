
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class State
{
    public enum STATE
    {
        IDLE, PATROL, PURSUE, ATTACK, SLEEP
    }

    public enum EVENT
    {
        ENTER, UPDATE, EXIT
    }

    public STATE name;
    protected EVENT stage;
    protected GameObject npc;
    protected Animator anim;
    protected Transform player;
    protected State nextState;
    protected NavMeshAgent agent;

    float visDist = 10.0f;
    float visAngle = 30.0f;
    float shootDist = 7.0f;

    public State(GameObject _npc, NavMeshAgent _agent,
                 Animator _anim, Transform _player)
    {
        npc = _npc;
        agent = _agent;
        anim = _anim;
        player = _player;
        stage = EVENT.ENTER;
    }

    public virtual void Enter()
    {
        stage = EVENT.UPDATE;
    }

    public virtual void Update()
    {
        stage = EVENT.UPDATE;
    }

    public virtual void Exit()
    {
        stage = EVENT.EXIT;
    }

    public State Process()
    {
        if (stage == EVENT.ENTER)
            Enter();

        if (stage == EVENT.UPDATE)
            Update();

        if (stage == EVENT.EXIT)
        {
            Exit();
            return nextState;
        }

        return this;
    }

    public bool CanSeePlayer()
    {
        Vector3 direction =
            player.position - npc.transform.position;

        float angle = Vector3.Angle(
            direction, npc.transform.forward
        );

        return direction.magnitude < visDist &&
               angle < visAngle;
    }

    public bool CanAttackPlayer()
    {
        Vector3 direction =
            player.position - npc.transform.position;

        return direction.magnitude < shootDist;
    }
}

public class Idle : State
{
    public Idle(GameObject npc, NavMeshAgent agent,
                Animator anim, Transform player)
        : base(npc, agent, anim, player)
    {
        name = STATE.IDLE;
    }

    public override void Enter()
    {
        agent.isStopped = true;
        anim.SetTrigger("isIdle");
        base.Enter();
    }

    public override void Update()
    {
        if (CanSeePlayer())
        {
            nextState = new Pursue(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
        else if (Random.Range(0, 100) < 10)
        {
            nextState = new Patrol(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        anim.ResetTrigger("isIdle");
        base.Exit();
    }
}

public class Patrol : State
{
    int currentIndex = -1;

    public Patrol(GameObject npc, NavMeshAgent agent,
                  Animator anim, Transform player)
        : base(npc, agent, anim, player)
    {
        name = STATE.PATROL;
        agent.speed = 2;
        agent.isStopped = false;
    }

    public override void Enter()
    {
        float lastDist = Mathf.Infinity;
        for(int i =0; i< GameEnviroment.Singleton.Checkpoints.Count; i++)
        {
            GameObject thisWP = GameEnviroment.Singleton.Checkpoints[i];
            float distance = Vector3.Distance(npc.transform.position, thisWP.transform.position);
            if(distance < lastDist)
            {
                currentIndex = i-1;
                lastDist = distance;
            }
        }
        anim.SetTrigger("isWalking");
        base.Enter();
    }

    public override void Update()
    {
        if (CanSeePlayer())
        {
            nextState = new Pursue(npc, agent, anim, player);
            stage = EVENT.EXIT;
            return;
        }

        List<GameObject> checkpoints =
            GameEnviroment.Singleton.Checkpoints;

        if (checkpoints.Count == 0)
            return;

        if (!agent.pathPending &&
            agent.remainingDistance < 1f)
        {
            currentIndex++;

            if (currentIndex >= checkpoints.Count)
                currentIndex = 0;
            else
                currentIndex++;
            
            agent.SetDestination(checkpoints[currentIndex].transform.position
            );
        }

        if (CanSeePlayer())
        {

        }
    }

    public override void Exit()
    {
        anim.ResetTrigger("isWalking");
        base.Exit();
    }
}

public class Pursue : State
{
    public Pursue(GameObject npc, NavMeshAgent agent,
                  Animator anim, Transform player)
        : base(npc, agent, anim, player)
    {
        name = STATE.PURSUE;
        agent.speed = 5;
        agent.isStopped = false;
    }

    public override void Enter()
    {
        anim.SetTrigger("isRunning");
        base.Enter();
    }

    public override void Update()
    {
        agent.SetDestination(player.position);

        if (agent.hasPath)
        {
            if (CanAttackPlayer())
            {
                nextState = new Attack(npc, agent, anim, player);
                stage = EVENT.EXIT;
            }
            else if (!CanSeePlayer())
            {
                nextState = new Patrol(npc, agent, anim, player);
                stage = EVENT.EXIT;
            }
        }
    }

    public override void Exit()
    {
        anim.ResetTrigger("isRunning");
        base.Exit();
    }
}

public class Attack : State
{
    float rotationSpeed = 2.0f;
    AudioSource shoot;

    public Attack(GameObject npc, NavMeshAgent agent,
                  Animator anim, Transform player)
        : base(npc, agent, anim, player)
    {
        name = STATE.ATTACK;
        shoot = npc.GetComponent<AudioSource>();
    }

    public override void Enter()
    {
        anim.SetTrigger("isShooting");
        agent.isStopped = true;

        if (shoot != null)
            shoot.Play();

        base.Enter();
    }

    public override void Update()
    {
        Vector3 direction =
            player.position - npc.transform.position;

        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            npc.transform.rotation = Quaternion.Slerp(
                npc.transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * rotationSpeed
            );
        }

        if (!CanAttackPlayer())
        {
            nextState = new Idle(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        anim.ResetTrigger("isShooting");

        if (shoot != null)
            shoot.Stop();

        base.Exit();
    }
}
