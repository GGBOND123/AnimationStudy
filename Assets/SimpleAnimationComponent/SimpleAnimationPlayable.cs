using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System;

public partial class SimpleAnimationPlayable : PlayableBehaviour
{
    LinkedList<QueuedState> m_StateQueue;
    /// <summary>
    /// 每个SimpleAnimationPlayable 都有一个 List<StateInfo> m_States，StateInfo中包含AnimationClipPlayable
    /// </summary>
    StateManagement m_States;

    protected Playable m_SimpleAnimationPlayable;
    protected Playable curSimpleAnimationPlayable { get { return m_SimpleAnimationPlayable; } }
    public Playable CurSimpleAnimationPlayable { get { return curSimpleAnimationPlayable; } }
    protected PlayableGraph graph { get { return curSimpleAnimationPlayable.GetGraph(); } }

    /// <summary>
    /// 每个SimpleAnimationPlayable 都有一个 AnimationMixerPlayable
    /// </summary>
    AnimationMixerPlayable m_Mixer;

    public System.Action onDone = null;


    bool m_Initialized;

    //是否将AnimationClipPlayable与AnimationMixerPlayable关联起来
    bool m_KeepStoppedPlayablesConnected = true;
    public bool keepStoppedPlayablesConnected
    {
        get { return m_KeepStoppedPlayablesConnected; }
        set
        {
            if (value != m_KeepStoppedPlayablesConnected)
            {
                m_KeepStoppedPlayablesConnected = value;
            }
        }
    }

    //void UpdateStoppedPlayablesConnections()
    //{
    //    for (int i = 0; i < m_States.Count; i++)
    //    {
    //        StateInfo state = m_States[i];
    //        if (state == null)
    //            continue;
    //        if (state.enabled)
    //            continue;
    //        if (keepStoppedPlayablesConnected)
    //        {
    //            ConnectInput(state.index);
    //        }
    //        else
    //        {
    //            DisconnectInput(state.index);
    //        }
    //    }
    //}

    public SimpleAnimationPlayable()
    { 
        m_States = new StateManagement();
        this.m_StateQueue = new LinkedList<QueuedState>();
    }

    public Playable GetInput(int index)
    {
        if (index >= m_Mixer.GetInputCount())
            return Playable.Null;

        return m_Mixer.GetInput(index);
    }

    /// <summary>
    /// 当拥有PlayableBehaviour的Playable被创建时，这个函数被调用。
    /// </summary>
    /// <param name="playable"></param>
    public override void OnPlayableCreate(Playable simpleAnimationPlayable)
    {
        Debug.Log("Playable被创建了！");
        m_SimpleAnimationPlayable = simpleAnimationPlayable;

        var mixer = AnimationMixerPlayable.Create(graph, 1, true);
        m_Mixer = mixer;

        curSimpleAnimationPlayable.SetInputCount(1);
        curSimpleAnimationPlayable.SetInputWeight(0, 1);
        graph.Connect(m_Mixer, 0, curSimpleAnimationPlayable, 0);
    }





    public IEnumerable<IState> GetStates()
    {
        return new PlayableIStateEnumerable(this);
    }

    public IState GetState(string name)
    {
        StateInfo state = m_States.FindState(name);
        if (state == null)
        {
            return null;
        }

        return new StateHandle(this, state.index, state.animationClipPlayable);
    }
    /// <summary>
    /// 根据AnimationClip创建AnimationClipPlayable，并设置对应Playable的初始化状态，并赋值到StateInfo中，并插入m_States中，通过keepStoppedPlayablesConnected判断是否连接到graph中。
    /// </summary>
    /// <param name="name"></param>
    /// <param name="clip"></param>
    /// <returns></returns>
    private StateInfo DoAddClip(string name, AnimationClip clip)
    {
        //Start new State
        StateInfo newState = m_States.InsertState();
        newState.Initialize(name, clip, clip.wrapMode);
        //Find at which input the state will be connected
        int index = newState.index;

        //Increase input count if needed
        if (index == m_Mixer.GetInputCount())  //返回该AnimationMixerPlayable的输入槽数量，该数量可在Create中指定。
        {
            m_Mixer.SetInputCount(index + 1);   //改变Playable 的输入槽数量。
        }

        var animationClipPlayable = AnimationClipPlayable.Create(graph, clip);

        animationClipPlayable.SetApplyFootIK(false);
        animationClipPlayable.SetApplyPlayableIK(false);
        
        if (!clip.isLooping || newState.wrapMode == WrapMode.Once)
        {
            animationClipPlayable.SetDuration(clip.length);
        }
        newState.SetPlayable(animationClipPlayable);
        newState.Pause();

        if (keepStoppedPlayablesConnected)
            ConnectInput(newState.index);

        return newState;
    }

    public bool AddClip(AnimationClip clip, string name)
    {
        StateInfo state = m_States.FindState(name);
        //已存在
        if (state != null)
        {
            Debug.LogError(string.Format("Cannot add state with name {0}, because a state with that name already exists", name));
            return false;
        }

        DoAddClip(name, clip);
        UpdateDoneStatus();
        InvalidateStates();

        return true;
    }

    public bool RemoveClip(string name)
    {
        StateInfo state = m_States.FindState(name);
        if (state == null)
        {
            Debug.LogError(string.Format("Cannot remove state with name {0}, because a state with that name doesn't exist", name));
            return false;
        }

        RemoveClones(state);
        InvalidateStates();
        m_States.RemoveState(state.index);
        return true;
    }

    public bool RemoveClip(AnimationClip clip)
    {
        InvalidateStates();
        return m_States.RemoveClip(clip);
    }

    public bool Play(string name)
    {
        StateInfo state = m_States.FindState(name);
        if (state == null)
        {
            Debug.LogError(string.Format("Cannot play state with name {0} because there is no state with that name", name));
            return false;
        }

        return Play(state.index);
    }

    private bool Play(int index)
    {
        for (int i = 0; i < m_States.Count; i++)
        {
            StateInfo state = m_States[i];
            if (state.index == index)
            {
                state.Enable();
                state.ForceWeight(1.0f);
            }
            else
            {
                DoStop(i);
            }
        }

        return true;
    }

    public bool PlayQueued(string name, QueueMode queueMode)
    {
        StateInfo state = m_States.FindState(name);
        if (state == null)
        {
            Debug.LogError(string.Format("Cannot queue Play to state with name {0} because there is no state with that name", name));
            return false;
        }

        return PlayQueued(state.index, queueMode);
    }

    bool PlayQueued(int index, QueueMode queueMode)
    {
        StateInfo newState = CloneState(index);

        if (queueMode == QueueMode.PlayNow)
        {
            Play(newState.index);
            return true;
        }

        m_StateQueue.AddLast(new QueuedState(StateInfoToHandle(newState), 0f));
        return true;
    }

    public void Rewind(string name)
    {
        StateInfo state = m_States.FindState(name);
        if (state == null)
        {
            Debug.LogError(string.Format("Cannot Rewind state with name {0} because there is no state with that name", name));
            return;
        }

        Rewind(state.index);
    }

    private void Rewind(int index)
    {
        m_States.SetStateTime(index, 0f);
    }

    public void Rewind()
    {
        for (int i = 0; i < m_States.Count; i++)
        {
            if (m_States[i] != null)
                m_States.SetStateTime(i, 0f);
        }
    }

    private void RemoveClones(StateInfo state)
    {
        var it = m_StateQueue.First;
        while (it != null)
        {
            var next = it.Next;

            StateInfo queuedState = m_States[it.Value.state.index];
            if (queuedState.parentState.index == state.index)
            {
                m_StateQueue.Remove(it);
                DoStop(queuedState.index);
            }

            it = next;
        }
    }

    public bool Stop(string name)
    {
        StateInfo state = m_States.FindState(name);
        if (state == null)
        {
            Debug.LogError(string.Format("Cannot stop state with name {0} because there is no state with that name", name));
            return false;
        }

        DoStop(state.index);

        UpdateDoneStatus();

        return true;
    }

    private void DoStop(int index)
    {
        StateInfo state = m_States[index];
        if (state == null)
            return;
        m_States.StopState(index, state.isClone);
        if (!state.isClone)
        {
            RemoveClones(state);
        }
    }

    public bool StopAll()
    {
        for (int i = 0; i < m_States.Count; i++)
        {
            DoStop(i);
        }

        CurSimpleAnimationPlayable.SetDone(true);

        return true;
    }

    public bool IsPlaying()
    {
        return m_States.AnyStatePlaying();
    }

    public bool IsPlaying(string stateName)
    {
        StateInfo state = m_States.FindState(stateName);
        if (state == null)
            return false;

        return state.enabled || IsClonePlaying(state);
    }

    private bool IsClonePlaying(StateInfo state)
    {
        for (int i = 0; i < m_States.Count; i++)
        {
            StateInfo otherState = m_States[i];
            if (otherState == null)
                continue;

            if (otherState.isClone && otherState.enabled && otherState.parentState.index == state.index)
            {
                return true;
            }
        }

        return false;
    }

    public int GetClipCount()
    {
        int count=0;
        for (int i = 0; i < m_States.Count; i++)
        {
            if (m_States[i] != null)
            {
                count++;
            }
        }
        return count;
    }

    private void SetupLerp(StateInfo state, float targetWeight, float time)
    {
        float travel = Mathf.Abs(state.weight - targetWeight);
        float newSpeed = time != 0f ? travel / time : Mathf.Infinity;

        // If we're fading to the same target as before but slower, assume CrossFade was called multiple times and ignore new speed
        if (state.fading && Mathf.Approximately(state.targetWeight, targetWeight) && newSpeed < state.fadeSpeed)
            return;

        state.FadeTo(targetWeight, newSpeed);
    }

    private bool Crossfade(int index, float time)
    {
        for (int i = 0; i < m_States.Count; i++)
        {
            StateInfo state = m_States[i];
            if (state == null)
                continue;

            if (state.index == index)
            {
                m_States.EnableState(index);
            }

            if (state.enabled == false)
                continue;

            float targetWeight = state.index == index ? 1.0f : 0.0f;
            SetupLerp(state, targetWeight, time);
        }

        return true;
    }

    private StateInfo CloneState(int index)
    {
        StateInfo original = m_States[index];
        string newName = original.stateName + "Queued Clone";
        StateInfo clone = DoAddClip(newName, original.clip);
        clone.SetAsCloneOf(new StateHandle(this, original.index, original.animationClipPlayable));
        return clone;
    }

    public bool Crossfade(string name, float time)
    {
        StateInfo state = m_States.FindState(name);
        if (state == null)
        {
            Debug.LogError(string.Format("Cannot crossfade to state with name {0} because there is no state with that name", name));
            return false;
        }

        if (time == 0f)
            return Play(state.index);

        return Crossfade(state.index, time);
    }

    public bool CrossfadeQueued(string name, float time, QueueMode queueMode)
    {
        StateInfo state = m_States.FindState(name);
        if (state == null)
        {
            Debug.LogError(string.Format("Cannot queue crossfade to state with name {0} because there is no state with that name", name));
            return false;
        }

        return CrossfadeQueued(state.index, time, queueMode);
    }

    private bool CrossfadeQueued(int index, float time, QueueMode queueMode)
    {
        StateInfo newState = CloneState(index);

        if (queueMode == QueueMode.PlayNow)
        {
            Crossfade(newState.index, time);
            return true;
        }

        m_StateQueue.AddLast(new QueuedState(StateInfoToHandle(newState), time));
        return true;
    }

    private bool Blend(int index, float targetWeight, float time)
    {
        StateInfo state = m_States[index];
        if (state.enabled == false)
            m_States.EnableState(index);

        if (time == 0f)
        {
            state.ForceWeight(targetWeight);
        }
        else
        {
            SetupLerp(state, targetWeight, time);
        }

        return true;
    }

    public bool Blend(string name, float targetWeight, float time)
    {
        StateInfo state = m_States.FindState(name);
        if (state == null)
        {
            Debug.LogError(string.Format("Cannot blend state with name {0} because there is no state with that name", name));
            return false;
        }

        return Blend(state.index, targetWeight, time);
    }

    public override void OnGraphStop(Playable playable)
    {
        //if the playable is not valid, then we are destroying, and our children won't be valid either
        if (!curSimpleAnimationPlayable.IsValid())
            return;

        for (int i = 0; i < m_States.Count; i++)
        {
            StateInfo state = m_States[i];
            if (state == null)
                continue;

            if (state.fadeSpeed == 0f && state.targetWeight == 0f)
            {
                Playable input = m_Mixer.GetInput(state.index);
                if (!input.Equals(Playable.Null))
                {
                    input.ResetTime(0f);
                }
            }
        }
    }

    /// <summary>
    /// 如果没有动画在播放中，将 playable 设置为播放完毕状态。
    /// </summary>
    private void UpdateDoneStatus()
    {
        if (!m_States.AnyStatePlaying())
        {
            //当前SimpleAnimationPlayable 是否播放完了
            bool wasDone = CurSimpleAnimationPlayable.IsDone();
            //设置 playable 是否播放完的标识符
            CurSimpleAnimationPlayable.SetDone(true);
            if (!wasDone && onDone != null)
            {
                onDone();
            }
        }

    }

    private void CleanClonedStates()
    {
        for (int i = m_States.Count-1; i >= 0; i--)
        {
            StateInfo state = m_States[i];
            if (state == null)
                continue;

            if (state.isReadyForCleanup)
            {
                Playable toDestroy = m_Mixer.GetInput(state.index);
                graph.Disconnect(m_Mixer, state.index);
                graph.DestroyPlayable(toDestroy);
                m_States.RemoveState(i);
            }
        }
    }

    private void DisconnectInput(int index)
    {
        if (keepStoppedPlayablesConnected)
        {
            m_States[index].Pause();
        }
        graph.Disconnect(m_Mixer, index);
    }
    /// <summary>
    /// 将AnimationClipPlayable与AnimationMixerPlayable关联起来。
    /// </summary>
    /// <param name="index"></param>
    private void ConnectInput(int index)
    {
        StateInfo state = m_States[index];
        graph.Connect(state.animationClipPlayable, 0, m_Mixer, state.index);
    }

    private void UpdateStates(float deltaTime)
    {
        bool mustUpdateWeights = false;
        float totalWeight = 0f;
        for (int i = 0; i < m_States.Count; i++)
        {
            StateInfo state = m_States[i];

            //Skip deleted states
            if (state == null)
            {
                continue;
            }

            //Update crossfade weight
            if (state.fading)
            {
                state.SetWeight(Mathf.MoveTowards(state.weight, state.targetWeight, state.fadeSpeed *deltaTime));
                if (Mathf.Approximately(state.weight, state.targetWeight))
                {
                    state.ForceWeight(state.targetWeight);
                    if (state.weight == 0f)
                    {
                        state.Stop();
                    }
                }
            }

            if (state.enabledDirty)
            {
                if (state.enabled)
                    state.Play();
                else
                    state.Pause();

                if (!keepStoppedPlayablesConnected)
                {
                    Playable input = m_Mixer.GetInput(i);
                    //if state is disabled but the corresponding input is connected, disconnect it
                    if (input.IsValid() && !state.enabled)
                    {
                        DisconnectInput(i);
                    }
                    else if (state.enabled && !input.IsValid())
                    {
                        ConnectInput(state.index);
                    }
                }
            }

            if (state.enabled && state.wrapMode == WrapMode.Once)
            {
                bool stateIsDone = state.isDone;
                float speed = state.speed;
                float time = state.GetTime();
                float duration = state.playableDuration;

                stateIsDone |= speed < 0f && time < 0f;
                stateIsDone |= speed >= 0f && time >= duration;
                if (stateIsDone)
                {
                    state.Stop();
                    state.Disable();
                    if (!keepStoppedPlayablesConnected)
                        DisconnectInput(state.index);

                }
            }

            totalWeight += state.weight;
            if (state.weightDirty)
            {
                mustUpdateWeights = true;
            }
            state.ResetDirtyFlags();
        }

        if (mustUpdateWeights)
        {
            bool hasAnyWeight = totalWeight > 0.0f;
            for (int i = 0; i < m_States.Count; i++)
            {
                StateInfo state = m_States[i];
                if (state == null)
                    continue;

                float weight = hasAnyWeight ? state.weight / totalWeight : 0.0f;
                m_Mixer.SetInputWeight(state.index, weight);
            }
        }
    }

    private float CalculateQueueTimes()
    {
        float longestTime = -1f;

        for (int i = 0; i < m_States.Count; i++)
        {
            StateInfo state = m_States[i];
            //Skip deleted states
            if (state == null || !state.enabled || !state.animationClipPlayable.IsValid())
                continue;

            if (state.wrapMode == WrapMode.Loop)
            {
                return Mathf.Infinity;
            }

            float speed = state.speed;
            float stateTime = m_States.GetStateTime(state.index);
            float remainingTime;
            if (speed > 0 )
            {
                remainingTime = (state.clip.length - stateTime) / speed;
            }
            else if(speed < 0 )
            {
                remainingTime = (stateTime) / speed;
            }
            else
            {
                remainingTime = Mathf.Infinity;
            }

            if (remainingTime > longestTime)
            {
                longestTime = remainingTime;
            }
        }

        return longestTime;
    }

    private void ClearQueuedStates()
    {
        using (var it = m_StateQueue.GetEnumerator())
        {
            while (it.MoveNext())
            {
                QueuedState queuedState = it.Current;
                m_States.StopState(queuedState.state.index, true);
            }
        }
        m_StateQueue.Clear();
    }

    private void UpdateQueuedStates()
    {
        bool mustCalculateQueueTimes = true;
        float remainingTime = -1f;

        var it = m_StateQueue.First;
        while(it != null)
        {
            if (mustCalculateQueueTimes)
            {
                remainingTime = CalculateQueueTimes();
                mustCalculateQueueTimes = false;
            }

            QueuedState queuedState = it.Value;

            if (queuedState.fadeTime >= remainingTime)
            {
                Crossfade(queuedState.state.index, queuedState.fadeTime);
                mustCalculateQueueTimes = true;
                m_StateQueue.RemoveFirst();
                it = m_StateQueue.First;
            }
            else
            {
                it = it.Next;
            }

        }
    }

    /// <summary>
    /// List<StateInfo> 中所有 StateInfo的 标志是否为最新时间  置为false
    /// </summary>
    void InvalidateStateTimes()
    {
        int count = m_States.Count;
        for (int i = 0; i < count; i++)
        {
            StateInfo state = m_States[i];
            if (state == null)
                continue;

            state.InvalidateTime();
        }
    }

    public override void PrepareFrame(Playable owner, FrameData data)
    {
        InvalidateStateTimes();

        UpdateQueuedStates();

        UpdateStates(data.deltaTime);

        //Once everything is calculated, update done status
        UpdateDoneStatus();

        CleanClonedStates();
    }

    public bool ValidateInput(int index, Playable input)
    {
        if (!ValidateIndex(index))
            return false;

        StateInfo state = m_States[index];
        if (state == null || !state.animationClipPlayable.IsValid() || state.animationClipPlayable.GetHandle() != input.GetHandle())   //传入的Playable和 m_States中StateInfo中的Playable 的PlayableHandle应该相等
            return false;

        return true;
    }

    public bool ValidateIndex(int index)
    {
        return index >= 0 && index < m_States.Count;
    }
}