using System;

// instantiate state manager in minion controller.
// the states can be implemented in the minion controller file since they are actually doing the important stuff.
// the state manager is also not minion controller independant, but that could have its own file.
// where should i handle the state transitions, in the player controller i guess in the main funcction i shall watch the inputs,
// and change states regarding to the inputs.
// But then i there have to implement a smaller state machine so i know which inputs to watch, and that makes no sense that way.
// nevermind at least the states and what to do in them are groupped together, and hopefully I dont have to prepare any special cases
// for different state changes

// knowing what to do when changing states
public class StateManager {
    // currently active state
    IState currentState;

    #region interfaces
    public IState CurrentState { get => currentState; }
    #endregion interfaces

    public StateManager(IState initState) {
        // The starting state is the idle state
        currentState = initState;
        initState.Enter();
    }

    // contains the logic between 
    public void ChangeToState(IState newState) {
        if (currentState is IdleState && newState is Walking) {
            // change from idle to walking state
            DefaultChange(newState);
        }
        else if (currentState is IdleState && newState is Attacking) {
            // change from idle to attacking state
            DefaultChange(newState);
        }
        else if (currentState is Walking && newState is Attacking) {
            // change from walkig to attacking state
            DefaultChange(newState);
        }
        else if (currentState is Walking && newState is IdleState) {
            // change from walking to idle, we have arrived
            DefaultChange(newState);
        }
        else if (currentState is Attacking && newState is Walking) {
            // change from attacking to walking, lost target while walking
            DefaultChange(newState);
        }
        else if (currentState is Attacking && newState is IdleState) {
            // change from attacking to idle, lost target while idle
            DefaultChange(newState);
        }
        else {
            throw new Exception("Invalid state transition");
        }
    }

    // what to do in Update
    public void Update() {
        currentState.Update();
    }

    // what to do in FixedUpdate
    public void FixedUpdate() {
        currentState.FixedUpdate();
    }

    // exit from prev state and enter the new state
    private void DefaultChange(IState newState) {
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
}

// what every state has in common
public interface IState {
    // Execute when entering state
    void Enter();
    // Execute when exiting state
    void Exit();
    // Execute when updating the state
    void Update();
    // Execute when fixed updating the state
    void FixedUpdate();
}
