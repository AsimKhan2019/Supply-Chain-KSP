using SupplyChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain.Sequencing
{
    public class SupplyChainProcess : IConfigNode, ISchedulable
    {
        public List<SupplyChainAction> sequence;
        public int currentIndex;

        public Guid pid;
        public string name;

        /*
         * Sequence Action Execution Flow:
         * 
         *  PreExecute -> Execute -> Complete
         *  
         * Preexecution: Wait on:
         *  - Action-specific conditions (i.e. Action.canExecute() == true)
         *  - all Vessels involved to become free
         *  - 
         * Execution: Actually begin the action and wait for completion.
         * Complete: Update the Sequence's state.
         * 
         * For any given Vessel, only one Sequence is allowed to Execute an Action involving it at any given time.
         */
         
        public enum ProcessStatus
        {
            INACTIVE = 0,           /* Process not scheduled to run. */
            WAITING_START = 1,      /* Process is scheduled to run as soon as initial conditions have been met */
            RUNNING = 2,            /* Process actions are in progress. */
            WAITING_VESSEL = 3,     /* Process is waiting on vessel lock. */
            WAITING_ACTION = 4      /* Process is waiting on action conditions. */
        }

        ProcessStatus status = ProcessStatus.INACTIVE;

        public SupplyChainProcess()
        {
            sequence = new List<SupplyChainAction>();
            conditions = new List<List<SequenceCondition>>();
            currentIndex = 0;
            pid = Guid.NewGuid();
            name = "";
        }

        public void schedulerUpdate()
        {
            switch (this.status)
            {
                case ProcessStatus.INACTIVE:
                    return; // nothing to do here.
                case ProcessStatus.RUNNING:
                    if (sequence[currentIndex].timeComplete <= Planetarium.GetUniversalTime() && !sequence[currentIndex].active)
                    {
                        sequence[currentIndex].linkVessel.unlockVessel(this);

                        currentIndex++;
                        if (currentIndex > this.sequence.Count)
                        {
                            Debug.Log("[SupplyChainProcess] Process " + pid.ToString() + " finished running.");
                            status = ProcessStatus.INACTIVE;
                            return;
                        }
                        Debug.Log("[SupplyChainProcess] Process " + pid.ToString() + " moving to action #" + currentIndex.ToString() + ".");
                        status = ProcessStatus.WAITING_ACTION;
                    }
                    break;
                case ProcessStatus.WAITING_START:
                    if (checkConditions())
                    {
                        currentIndex = 0;
                        status = ProcessStatus.WAITING_ACTION;
                        /* Update all preconditions (for things like Periodic conditions). */
                        foreach(List<SequenceCondition> condSet in conditions)
                        {
                            foreach(SequenceCondition cond in condSet)
                            {
                                cond.onProcessStart(this);
                            }
                        }

                        Debug.Log("[SupplyChainProcess] Process " + pid.ToString() + " is starting.");
                    }
                    break;
                case ProcessStatus.WAITING_ACTION:
                    if (sequence[currentIndex].canExecute())
                    {
                        Debug.Log("[SupplyChainProcess] Process " + pid.ToString() + " taking lock for vessel " + sequence[currentIndex].linkVessel.vessel.name + ".");
                        status = ProcessStatus.WAITING_VESSEL;
                        sequence[currentIndex].linkVessel.lockVessel(this);
                    }
                    break;
                case ProcessStatus.WAITING_VESSEL:
                    break; /* This state is handled separately. */
            }
        }

        public void onLockTaken(VesselData vd)
        {
            Debug.Assert(this.status == ProcessStatus.WAITING_VESSEL, "[SupplyChain] SupplyChainProcess " + this.pid.ToString() + ": onLockTaken called while not waiting on vessel!");

            this.status = ProcessStatus.RUNNING;
            /* Fire next action-- we (must) always pass thru the WAITING_ACTION and WAITING_VESSEL state before beginning to execute an action. */

            Debug.Log("[SupplyChainProcess] Process " + pid.ToString() + " has taken lock for vessel " + vd.vessel.name + ", executing action #" + currentIndex.ToString());
            if(!sequence[currentIndex].startAction())
            {
                /* Vessel not in correct state anymore-- give up lock and go back to waiting. */
                vd.unlockVessel(this);
                status = ProcessStatus.WAITING_ACTION;
            }
        }
        
        /* each List<SequenceCondition> represents a set of conditions that must all be met for that condition set to be true.
         * 
         * If any condition set is true then the whole sequence will fire.
         */ 
        public List<List<SequenceCondition>> conditions;
        
        public bool checkConditions()
        {
            if (conditions == null || conditions.Count == 0)
                return true;

            foreach(List<SequenceCondition> conditionSet in conditions)
            {
                bool status = true;
                foreach(SequenceCondition condition in conditionSet)
                {
                    status = status && condition.check();
                }

                if(status)
                    return true;
            }

            return false;
        }
        
        public void startSequence()
        {
            currentIndex = 0;
            status = ProcessStatus.WAITING_START;
        }

        public void Load(ConfigNode node)
        {
            /* Load action ConfigNodes in order.
             * I'm unsure if node.GetNodes() will return the nodes in sequence,
             * so we'll have to sort it first.
             */

            name = node.GetValue("pid");
            pid = new Guid(node.GetValue("pid"));

            List<ConfigNode> nodes = new List<ConfigNode>(node.GetNodes("Action"));
            nodes.Sort(
                (ConfigNode comp1, ConfigNode comp2) =>
                {
                    int idx1 = 0;
                    int idx2 = 0;
                    comp1.TryGetValue("idx", ref idx1);
                    comp2.TryGetValue("idx", ref idx2);

                    return idx1.CompareTo(idx2);
                }
            );

            foreach(ConfigNode actNode in nodes)
            {
                int idx = 0;
                actNode.TryGetValue("idx", ref idx);
                string type = actNode.GetValue("type");
                if(type == "SupplyLink")
                {
                    SupplyLink act = SupplyChainController.getLinkByGuid(new Guid(actNode.GetValue("id")));
                } else if (type == "ResourceTransfer")
                {
                    ResourceTransferAction act = new ResourceTransferAction();
                    act.Load(actNode);
                }
            }

            List<ConfigNode> setNodes = new List<ConfigNode>(node.GetNodes("ConditionSet"));
            foreach(ConfigNode setNode in setNodes)
            {
                List<SequenceCondition> condSet = new List<SequenceCondition>();

                List<ConfigNode> condNodes = new List<ConfigNode>(node.GetNodes("Condition"));
                foreach(ConfigNode condNode in condNodes)
                {
                    SequenceCondition condition = SequenceCondition.LoadFactory(condNode);
                    if(condition != null)
                        condSet.Add(condition);
                }

                conditions.Add(condSet);
            }
        }

        public void Save(ConfigNode node)
        {
            /* Save basic data. */
            node.AddValue("name", this.name);
            node.AddValue("pid", this.pid.ToString());

            /* Save actions. */
            for (int i = 0; i < this.sequence.Count; i++)
            {
                ConfigNode act = node.AddNode("Action");
                act.AddValue("idx", i);

                if(this.sequence[i].GetType() != typeof(SupplyLink))
                {
                    this.sequence[i].Save(act);
                } else
                {
                    act.AddValue("type", "SupplyLink");
                    act.AddValue("id", ((SupplyLink)this.sequence[i]).id.ToString());
                }
            }

            /* Save preconditions */
            foreach(List<SequenceCondition> condSet in conditions)
            {
                ConfigNode setNode = node.AddNode("ConditionSet");
                foreach(SequenceCondition cond in condSet)
                {
                    ConfigNode condNode = node.AddNode("Condition");
                    cond.Save(condNode);
                }
            }
        }
    }
}
