using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// State action (entry or exit)
    /// </summary>
    public delegate Task StateAction();

    /// <summary>
    /// Transition condition/guard
    /// </summary>
    public delegate Task<bool> TransitionGuard();

    /// <summary>
    /// State definition
    /// </summary>
    public class State
    {
        public string Name { get; set; }
        public StateAction EntryAction { get; set; }
        public StateAction ExitAction { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public override bool Equals(object obj)
        {
            return obj is State state && state.Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// State transition
    /// </summary>
    public class Transition
    {
        public string Event { get; set; }
        public State TargetState { get; set; }
        public TransitionGuard Guard { get; set; }
        public StateAction Action { get; set; }
    }

    /// <summary>
    /// State machine interface
    /// </summary>
    public interface IStateMachine
    {
        State CurrentState { get; }
        void AddState(State state);
        void AddTransition(State fromState, string @event, State toState, TransitionGuard guard = null, StateAction action = null);
        Task<bool> FireEventAsync(string @event);
        IEnumerable<Transition> GetAvailableTransitions();
        IEnumerable<State> GetAllStates();
    }

    /// <summary>
    /// State machine implementation
    /// </summary>
    public class StateMachine : IStateMachine
    {
        private State _currentState;
        private readonly Dictionary<string, State> _states = new();
        private readonly Dictionary<State, List<Transition>> _transitions = new();
        private readonly object _lock = new();
        private readonly EventBus _eventBus;

        public State CurrentState
        {
            get
            {
                lock (_lock)
                {
                    return _currentState;
                }
            }
        }

        public StateMachine(State initialState, EventBus eventBus = null)
        {
            _currentState = initialState;
            _eventBus = eventBus;
            _states[initialState.Name] = initialState;
            _transitions[initialState] = new();
        }

        public void AddState(State state)
        {
            lock (_lock)
            {
                _states[state.Name] = state;
                if (!_transitions.ContainsKey(state))
                {
                    _transitions[state] = new();
                }
            }
        }

        public void AddTransition(State fromState, string @event, State toState, TransitionGuard guard = null, StateAction action = null)
        {
            lock (_lock)
            {
                if (!_states.ContainsKey(fromState.Name))
                    throw new InvalidOperationException($"State '{fromState.Name}' not registered");
                if (!_states.ContainsKey(toState.Name))
                    throw new InvalidOperationException($"State '{toState.Name}' not registered");

                var transition = new Transition
                {
                    Event = @event,
                    TargetState = toState,
                    Guard = guard,
                    Action = action
                };

                _transitions[fromState].Add(transition);
            }
        }

        public async Task<bool> FireEventAsync(string @event)
        {
            lock (_lock)
            {
                if (!_transitions.TryGetValue(_currentState, out var availableTransitions))
                {
                    return false;
                }

                var transition = availableTransitions.Find(t => t.Event == @event);
                if (transition == null)
                {
                    return false;
                }

                // Check guard condition
                if (transition.Guard != null && !transition.Guard().Result)
                {
                    return false;
                }

                return TransitionTo(transition, @event).Result;
            }
        }

        public IEnumerable<Transition> GetAvailableTransitions()
        {
            lock (_lock)
            {
                if (_transitions.TryGetValue(_currentState, out var transitions))
                {
                    return new List<Transition>(transitions);
                }
                return new List<Transition>();
            }
        }

        public IEnumerable<State> GetAllStates()
        {
            lock (_lock)
            {
                return new List<State>(_states.Values);
            }
        }

        private async Task<bool> TransitionTo(Transition transition, string @event)
        {
            try
            {
                // Exit current state
                if (_currentState.ExitAction != null)
                {
                    await _currentState.ExitAction();
                }

                // Execute transition action
                if (transition.Action != null)
                {
                    await transition.Action();
                }

                // Enter target state
                var previousState = _currentState;
                _currentState = transition.TargetState;

                if (_currentState.EntryAction != null)
                {
                    await _currentState.EntryAction();
                }

                // Publish event
                _eventBus?.PublishEvent(new Event
                {
                    EventType = "StateTransition",
                    Data = new
                    {
                        FromState = previousState.Name,
                        ToState = _currentState.Name,
                        Event = @event,
                        Timestamp = DateTime.UtcNow
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                _eventBus?.PublishEvent(new Event
                {
                    EventType = "StateTransitionFailed",
                    Data = new { Error = ex.Message, Event = @event }
                });
                return false;
            }
        }

        /// <summary>
        /// Visualize state machine as graph (Graphviz DOT format)
        /// </summary>
        public string VisualizeDotFormat()
        {
            var lines = new List<string>();
            lines.Add("digraph StateMachine {");
            lines.Add("  rankdir=LR;");

            lock (_lock)
            {
                // Add states
                foreach (var state in _states.Values)
                {
                    var style = state.Name == _currentState.Name ? "[shape=box, style=filled, fillcolor=lightblue]" : "[shape=box]";
                    lines.Add($"  {state.Name} {style};");
                }

                // Add transitions
                foreach (var (state, transitions) in _transitions)
                {
                    foreach (var transition in transitions)
                    {
                        lines.Add($"  {state.Name} -> {transition.TargetState.Name} [label=\"{transition.Event}\"];");
                    }
                }
            }

            lines.Add("}");
            return string.Join("\n", lines);
        }
    }

    /// <summary>
    /// Boot state machine factory
    /// </summary>
    public static class BootStateMachineFactory
    {
        public static StateMachine CreateBootStateMachine(EventBus eventBus = null)
        {
            var offState = new State { Name = "Off" };
            var biosState = new State { Name = "BIOS" };
            var loaderState = new State { Name = "Loader" };
            var kernelState = new State { Name = "Kernel" };
            var servicesState = new State { Name = "Services" };
            var readyState = new State { Name = "Ready" };

            var sm = new StateMachine(offState, eventBus);

            sm.AddState(biosState);
            sm.AddState(loaderState);
            sm.AddState(kernelState);
            sm.AddState(servicesState);
            sm.AddState(readyState);

            sm.AddTransition(offState, "PowerOn", biosState);
            sm.AddTransition(biosState, "BiosDone", loaderState);
            sm.AddTransition(loaderState, "LoaderDone", kernelState);
            sm.AddTransition(kernelState, "KernelReady", servicesState);
            sm.AddTransition(servicesState, "ServicesReady", readyState);

            // Allow reboot from any state
            foreach (var state in sm.GetAllStates())
            {
                if (state != offState)
                {
                    sm.AddTransition(state, "Shutdown", offState);
                }
            }

            return sm;
        }

        public static StateMachine CreateUpdateStateMachine(EventBus eventBus = null)
        {
            var checkState = new State { Name = "Check" };
            var downloadState = new State { Name = "Download" };
            var verifyState = new State { Name = "Verify" };
            var stageState = new State { Name = "Stage" };
            var installState = new State { Name = "Install" };
            var activateState = new State { Name = "Activate" };
            var cleanupState = new State { Name = "Cleanup" };
            var doneState = new State { Name = "Done" };

            var sm = new StateMachine(checkState, eventBus);

            sm.AddState(downloadState);
            sm.AddState(verifyState);
            sm.AddState(stageState);
            sm.AddState(installState);
            sm.AddState(activateState);
            sm.AddState(cleanupState);
            sm.AddState(doneState);

            sm.AddTransition(checkState, "UpdateAvailable", downloadState);
            sm.AddTransition(downloadState, "DownloadComplete", verifyState);
            sm.AddTransition(verifyState, "VerifyOk", stageState);
            sm.AddTransition(stageState, "StagingComplete", installState);
            sm.AddTransition(installState, "InstallComplete", activateState);
            sm.AddTransition(activateState, "ActivationComplete", cleanupState);
            sm.AddTransition(cleanupState, "CleanupDone", doneState);

            // Allow rollback
            sm.AddTransition(verifyState, "VerifyFailed", checkState);
            sm.AddTransition(stageState, "StagingFailed", checkState);

            return sm;
        }
    }
}
