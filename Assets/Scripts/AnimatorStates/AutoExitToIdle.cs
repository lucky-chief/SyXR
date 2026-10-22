using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoExitToIdle : StateMachineBehaviour
{
    public string targetState = "Idle";
    // public float minTime = 2f;
    // public float maxTime = 5f;

    // private float elapsedTime = 0f;
    // private float waitTime = 2f;
    private bool isTrasitioning = false;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // waitTime = Random.Range(minTime, maxTime);
        // elapsedTime = 0f;
        isTrasitioning = false;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // elapsedTime += Time.deltaTime;
        if (isTrasitioning == false)
        {
            isTrasitioning = true;
            animator.CrossFade(targetState, 0.1f);
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
