﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using Port = UnityEditor.Experimental.GraphView.Port;

public class Machine : Interactable {
    [SerializeField] public Recipe recipe;
    // private int _maxInputPorts;
    // private int _minInputPorts;
    // private int _maxOutputPorts;
    // private int _minOutputPorts;
    // public int Perimeter;
    // public int MaxStorage = 1;

    [NonSerialized] public List<OutputPort> OutputPorts = new List<OutputPort>();
    [NonSerialized] public List<InputPort> InputPorts = new List<InputPort>();
    private int _ticksSinceProduced;
    private bool _pokedThisTick;
    protected static Conductor OurConductor;
    public List<Resource> OutputBuffer { get; private set; }
    public List<Resource> storage { get; private set; }

    public bool ShouldPrint;
    public bool ShouldBreak;

    protected void Awake() {
        if (recipe.InResources.Length == 0) {
            // _maxInputPorts = 0;
            // _minInputPorts = 0;
        }

        if (recipe.OutResources.Length == 0) {
            // _maxOutputPorts = 0;
            // _minOutputPorts = 0;
        }

        OutputBuffer = new List<Resource>();
        storage = new List<Resource>();
        recipe.Initiate();
    }
    
    void Start() {
        if(OurConductor == null) OurConductor = FindObjectOfType<Conductor>();
    }

    bool _shouldPrint() {
        return ShouldPrint;
        // return transform.name == Helper.Consts.NAME;
    }

    bool _shouldBreak() {
        return ShouldBreak;
    }

    private static void foreachMachine(List<MachinePort> portList, Action<Machine> func) {
        foreach (MachinePort i in portList) {
            var inputMachine = i.ConnectedMachine;
            if (inputMachine) {
                func(inputMachine);
            }
        }
    }

    private bool _checkEnoughInput() {
        var actualInputs = new List<Resource>();
        foreachMachine(new List<MachinePort>(InputPorts), m => actualInputs.AddRange(m.OutputBuffer));
        bool ret = recipe.CheckInputs(actualInputs);
        if (_shouldPrint()) {
            print("Input resources: ");
            foreach (Resource i in actualInputs) { print(i);}
            print("Enough input: "  +ret);
        }

        return ret;
    }

    public void ClearOutput() {
        OutputBuffer.Clear();
    }

    public void MoveHere(Resource r, bool destroyOnComplete) {
        var position = transform.position;
        var instantiatePos = new Vector3(position.x, position.y, r.gameObject.transform.position.z);
        r.MySmoothSprite.Move(instantiatePos, destroyOnComplete);
    }

    protected void MoveResourcesIn() {
        foreachMachine(new List<MachinePort>(InputPorts), m => {
            OutputBuffer.AddRange(m.OutputBuffer);
            m.OutputBuffer.Clear();
        });
        
        foreach (Resource r in OutputBuffer) {
            MoveHere(r, _shouldDestroyInputs());
        }
    }

    protected virtual void CreateOutput() {
        var position = transform.position;
        var resourcesToCreate = recipe.outToList();
        foreach (Resource r in resourcesToCreate) {
            var instantiatePos = new Vector3(position.x, position.y, r.transform.position.z);
            var newObj = Instantiate(r.transform, instantiatePos, transform.rotation);
            if (_shouldPrint()) {
                print("Adding new resource: " + r);
            }
            OutputBuffer.Add(newObj.GetComponent<Resource>());
        }
    }
    
    // Returns true if the machine destroys its input resources after moving them in.
    protected virtual bool _shouldDestroyInputs() {
        return recipe.CreatesNewOutput;
    }

    public void MoveAndDestroy() {
        //Foreach resource in each port's input buffer, move to this machine
        foreachMachine(new List<MachinePort>(InputPorts), m => {
            foreach (Resource resource in m.OutputBuffer) {
                if (_shouldPrint()) {
                    print("moving resource: " + m + resource);
                }
                MoveHere(resource, _shouldDestroyInputs());
            }
        });
        //Empty the output list of the input machines
        if (_shouldPrint()) {
            print("Emptying input ports' output");
        }
        foreachMachine(new List<MachinePort>(InputPorts), m => m.ClearOutput());
        //Create new resources based on the old ones
        if (_shouldPrint()) {
            print("Creating output");
        }
        CreateOutput();
    }

    protected virtual void _produce() {
        if (recipe.CreatesNewOutput) {
            if (_shouldPrint()) {
                print("moving and destroying");
            }
            MoveAndDestroy();
        } else {
            MoveResourcesIn();
        }
    }

    public void PrepareTick() {
        _pokedThisTick = false;
    }

    public void Tick() {
        if (!_pokedThisTick) {
            _pokedThisTick = true;
            bool enoughInput = _checkEnoughInput();
            if (_shouldPrint()) {
                print("Enough ticks: " + (_ticksSinceProduced >= recipe.ticks));
            }

            if (enoughInput && _ticksSinceProduced >= recipe.ticks) {
                _produce();
                _ticksSinceProduced = 0;
            }
            else {
                _ticksSinceProduced++;
            }
            foreachMachine(new List<MachinePort>(InputPorts), m => m.Tick());
        }

        if (_shouldBreak()) {
            Debug.Break();
        }
    }

    public void OnDrawGizmos() {
        // if (storage != null && OutputBuffer != null) {
        //     Handles.Label(
        //         transform.position,
        //         "" + OutputBuffer.Count
        //     );
        // }

        // Handles.Label(
        //     transform.position + new Vector3(0, -0.2f, 0),
        //     "" + _ticksSinceProduced
        // );
        Vector3 curPos = transform.position + new Vector3(0.1f, 0.1f, 0);
        foreachMachine(new List<MachinePort>(OutputPorts), m => {
            Vector3 direction = m.transform.position +new Vector3(0.1f, 0.1f, 0) - curPos;
            Helper.DrawArrow(curPos, direction, Color.green);
        });
        // foreachMachine(new List<MachinePort>(InputPorts), m => {
        //     Vector3 direction = curPos-m.transform.position - new Vector3(0.1f, 0.1f, 0);
        //     Helper.DrawArrow(m.transform.position, direction, Color.blue);
        // });
    }

    public int GetNumOutputMachines() {
        int ret = 0;
        foreach (OutputPort p in OutputPorts) {
            if (p.ConnectedMachine != null) ret++;
        }

        return ret;
    }

    public void AddOutputMachine(Machine m, Vector3 pos) {
        OutputPort newPort = OurConductor.InstantiateOutputPort(pos, transform);
        newPort.ConnectedMachine = m;
        OutputPorts = new List<OutputPort>();
        OutputPorts.Add(newPort);
    }
    
    public void AddInputMachine(Machine m, Vector3 pos) {
        InputPort newPort = OurConductor.InstantiateInputPort(pos, transform);
        newPort.ConnectedMachine = m;
        InputPorts = new List<InputPort>();
        InputPorts.Add(newPort);
    }

    public override void OnInteract(PlayerController p) {
        // throw new NotImplementedException();
    }

    public override void OnDeInteract(PlayerController p) {
        Vector3 newPos = p.transform.position;
        print(p.OnInteractable(newPos));
        Interactable nextInteractable = p.OnInteractable(newPos);
        Machine outMachine;
        if (nextInteractable == null) {
            Vector3 direction = newPos-transform.position;
            float angleRot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, 0, angleRot);
            Vector2 conveyorPos = new Vector2(newPos.x, newPos.y);
            outMachine = Conductor.Instance.InstantiateConveyor(conveyorPos, rotation);
        } else {
            outMachine = nextInteractable.GetComponent<Machine>();
        }
        Vector3 portPos = (transform.position + newPos) / 2;
        outMachine.AddInputMachine(this, portPos);
        AddOutputMachine(outMachine, portPos);
    }
}