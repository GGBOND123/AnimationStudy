using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System;

public partial class SimpleAnimationPlayable : PlayableBehaviour
{
    private int m_StatesVersion = 0;

    /// <summary>
    /// 增/删Clip 都让m_StatesVersion++ 
    /// </summary>
    private void InvalidateStates() { m_StatesVersion++; }
    private class PlayableIStateEnumerable: IEnumerable<IState>
    {
        private SimpleAnimationPlayable m_SimpleAnimationPlayable;
        public PlayableIStateEnumerable(SimpleAnimationPlayable simpleAnimationPlayable)
        {
            m_SimpleAnimationPlayable = simpleAnimationPlayable;
        }


        public IEnumerator<IState> GetEnumerator()
        {
            return new PlayableIStateEnumerator(m_SimpleAnimationPlayable);
        }



        //用于foreach
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new PlayableIStateEnumerator(m_SimpleAnimationPlayable);
        }

        class PlayableIStateEnumerator : IEnumerator<IState>
        {
            private int m_Index = -1;
            private int m_Version;
            private SimpleAnimationPlayable m_SimpleAnimationPlayable;
            public PlayableIStateEnumerator(SimpleAnimationPlayable simpleAnimationPlayable)
            {
                m_SimpleAnimationPlayable = simpleAnimationPlayable;
                m_Version = m_SimpleAnimationPlayable.m_StatesVersion;
                Reset();
            }

            private bool IsValid() { return m_SimpleAnimationPlayable != null && m_Version == m_SimpleAnimationPlayable.m_StatesVersion; }

            IState GetCurrentHandle(int index)
            {
                if (!IsValid())
                    throw new InvalidOperationException("The collection has been modified, this Enumerator is invalid");

                if (index < 0 || index >= m_SimpleAnimationPlayable.m_States.Count)
                    throw new InvalidOperationException("Enumerator is invalid");

                StateInfo state = m_SimpleAnimationPlayable.m_States[index];
                if (state == null)
                    throw new InvalidOperationException("Enumerator is invalid");

                return new StateHandle(m_SimpleAnimationPlayable, state.index, state.animationClipPlayable);
            }

            object IEnumerator.Current { get { return GetCurrentHandle(m_Index); } }

            IState IEnumerator<IState>.Current { get { return GetCurrentHandle(m_Index); } }

            public void Dispose() { }

            public bool MoveNext()
            {
                if (!IsValid())
                    throw new InvalidOperationException("The collection has been modified, this Enumerator is invalid");

                do
                { m_Index++; } while (m_Index < m_SimpleAnimationPlayable.m_States.Count && m_SimpleAnimationPlayable.m_States[m_Index] == null);

                return m_Index < m_SimpleAnimationPlayable.m_States.Count;
            }

            public void Reset()
            {
                if (!IsValid())
                    throw new InvalidOperationException("The collection has been modified, this Enumerator is invalid");
                m_Index = -1;
            }
        }
    }
    
    public interface IState
    {
        bool IsValid();

        bool enabled { get; set; }

        float time { get; set; }

        float normalizedTime { get; set; }

        float speed { get; set; }

        string name { get; set; }

        float weight { get; set; }

        float length { get; }

        AnimationClip clip { get; }

        WrapMode wrapMode { get; }
    }

    //通过SimpleAnimationPlayable 的 List<StateInfo> 和 m_Index索引 获取对应的StateInfo，再通过StateInfo 的接口操作 Playable 。
    public class StateHandle : IState
    {
        public int index { get { return m_Index; } }

        private SimpleAnimationPlayable m_SimpleAnimationPlayable;
        /// <summary>
        /// SimpleAnimationPlayable 中 List<StateInfo>的索引 
        /// </summary>
        private int m_Index;
        private Playable m_AnimationClipPlayable;


        public StateHandle(SimpleAnimationPlayable s, int index, Playable animationClipPlayable)
        {
            m_SimpleAnimationPlayable = s;
            m_Index = index;
            m_AnimationClipPlayable = animationClipPlayable;
        }

        public bool IsValid()
        {
            return m_SimpleAnimationPlayable.ValidateInput(m_Index, m_AnimationClipPlayable);
        }

        public bool enabled
        {
            get
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                return m_SimpleAnimationPlayable.m_States[m_Index].enabled;
            }

            set
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                if (value)
                    m_SimpleAnimationPlayable.m_States.EnableState(m_Index);
                else
                    m_SimpleAnimationPlayable.m_States.DisableState(m_Index);

            }
        }

        public float time
        {
            get
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                return m_SimpleAnimationPlayable.m_States.GetStateTime(m_Index);
            }
            set
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                m_SimpleAnimationPlayable.m_States.SetStateTime(m_Index, value);
            }
        }

        public float normalizedTime
        {
            get
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");

                float length = m_SimpleAnimationPlayable.m_States.GetClipLength(m_Index);
                if (length == 0f)
                    length = 1f;

                return m_SimpleAnimationPlayable.m_States.GetStateTime(m_Index) / length;
            }
            set
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");

                float length = m_SimpleAnimationPlayable.m_States.GetClipLength(m_Index);
                if (length == 0f)
                    length = 1f;

                m_SimpleAnimationPlayable.m_States.SetStateTime(m_Index, value *= length);
            }
        }

        public float speed
        {
            get
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                return m_SimpleAnimationPlayable.m_States.GetStateSpeed(m_Index);
            }
            set
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                m_SimpleAnimationPlayable.m_States.SetStateSpeed(m_Index, value);
            }
        }

        public string name
        {
            get
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                return m_SimpleAnimationPlayable.m_States.GetStateName(m_Index);
            }
            set
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                if (value == null)
                    throw new System.ArgumentNullException("A null string is not a valid name");
                m_SimpleAnimationPlayable.m_States.SetStateName(m_Index, value);
            }
        }

        public float weight
        {
            get
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                return m_SimpleAnimationPlayable.m_States[m_Index].weight;
            }
            set
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                if (value < 0)
                    throw new System.ArgumentException("Weights cannot be negative");

                m_SimpleAnimationPlayable.m_States.SetInputWeight(m_Index, value);
            }
        }

        public float length
        {
            get
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                return m_SimpleAnimationPlayable.m_States.GetStateLength(m_Index);
            }
        }

        public AnimationClip clip
        {
            get
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                return m_SimpleAnimationPlayable.m_States.GetStateClip(m_Index);
            }
        }

        public WrapMode wrapMode
        {
            get
            {
                if (!IsValid())
                    throw new System.InvalidOperationException("This StateHandle is not valid");
                return m_SimpleAnimationPlayable.m_States.GetStateWrapMode(m_Index);
            }
        }


    }









    /// <summary>
    /// 包含并操作AnimationClipPlayable的类，  通过 PlayableOutputExtensions 获取Playable的一些信息；或者通过该类操作 Playable。
    /// </summary>
    private class StateInfo
    {
        public void Initialize(string name, AnimationClip clip, WrapMode wrapMode)
        {
            m_ClipPlayableStateName = name;
            m_ClipPlayableStateClip = clip;
            m_ClipPlayableStateWrapMode = wrapMode;
        }
        /// <summary>
        /// Returns the current local time of the Playable.
        /// </summary>
        /// <returns></returns>
        public float GetTime()
        {
            if (m_TimeIsUpToDate)
                return m_Time;
            //Returns the current local time of the Playable.
            m_Time = (float)m_AnimationClipPlayable.GetTime();
            m_TimeIsUpToDate = true;
            return m_Time;
        }
        /// <summary>
        /// Changes the current local time of the Playable.  同时设置播放完成标志。
        /// </summary>
        /// <param name="newTime"></param>
        public void SetTime(float newTime)
        {
            m_Time = newTime;
            m_AnimationClipPlayable.ResetTime(m_Time);
            m_AnimationClipPlayable.SetDone(m_Time >= m_AnimationClipPlayable.GetDuration());  //Returns the duration of the Playable.
        }
        /// <summary>
        /// State播放前，标志m_Enabled为true
        /// </summary>
        public void Enable()
        {
            if (m_Enabled)
                return;

            m_EnabledDirty = true;
            m_Enabled = true;
        }

        public void Disable()
        {
            if (m_Enabled == false)
                return;

            m_EnabledDirty = true;
            m_Enabled = false;
        }
        /// <summary>
        /// 设置PlayState状态
        /// </summary>
        public void Pause()
        {
            m_AnimationClipPlayable.Pause();
        }
        /// <summary>
        /// 设置PlayState状态
        /// </summary>
        public void Play()
        {
            m_AnimationClipPlayable.SetPlayState(PlayState.Playing);
        }

        public void Stop()
        {
            m_FadeSpeed = 0f;
            ForceWeight(0.0f);
            Disable();
            SetTime(0.0f);
            m_AnimationClipPlayable.SetDone(false);
            if (isClone)
            {
                m_ReadyForCleanup = true;
            }
        }

        public void ForceWeight(float weight)
        {
           m_TargetWeight = weight;
           m_Fading = false;
           m_FadeSpeed = 0f;
           SetWeight(weight);
        }

        public void SetWeight(float weight)
        {
            m_Weight = weight;
            m_WeightDirty = true;
        }

        public void FadeTo(float weight, float speed)
        {
            m_Fading = Mathf.Abs(speed) > 0f;
            m_FadeSpeed = speed;
            m_TargetWeight = weight;
        }

        /// <summary>
        /// 递归清理当前 Playable和output。
        /// </summary>
        public void DestroyPlayable()
        {
            //	Playable 是否有效
            if (m_AnimationClipPlayable.IsValid())
            {
                m_AnimationClipPlayable.GetGraph().DestroySubgraph(m_AnimationClipPlayable);   
            }
        }

        public void SetAsCloneOf(StateHandle handle)
        {
            m_ParentState = handle;
            m_IsClone = true;
        }

        public bool enabled
        {
            get { return m_Enabled; }
        }

        private bool m_Enabled;

        public int index
        {
            get { return m_Index; }
            set
            {
                Debug.Assert(m_Index == 0, "Should never reassign Index");
                m_Index = value;
            }
        }

        private int m_Index;

        public string stateName
        {
            get { return m_ClipPlayableStateName; }
            set { m_ClipPlayableStateName = value; }
        }

        private string m_ClipPlayableStateName;

        public bool fading
        {
            get { return m_Fading; }
        }

        private bool m_Fading;


        private float m_Time;

        public float targetWeight
        {
            get { return m_TargetWeight; }
        }

        private float m_TargetWeight;

        public float weight
        {
            get { return m_Weight; }
        }

        float m_Weight;

        public float fadeSpeed
        {
            get { return m_FadeSpeed; }
        }
        /// <summary>
        /// 两动作插值时的速度
        /// </summary>
        float m_FadeSpeed;

        public float speed
        {
            get { return (float)m_AnimationClipPlayable.GetSpeed(); }
            set { m_AnimationClipPlayable.SetSpeed(value); }
        }

        public float playableDuration
        {
            get { return (float)m_AnimationClipPlayable.GetDuration(); }
        }

        public AnimationClip clip
        {
            get { return m_ClipPlayableStateClip; }
        }

        private AnimationClip m_ClipPlayableStateClip;

        public void SetPlayable(Playable animationClipPlayable)
        {
            m_AnimationClipPlayable = animationClipPlayable;
        }

        public bool isDone { get { return m_AnimationClipPlayable.IsDone(); } }

        public Playable animationClipPlayable
        {
            get { return m_AnimationClipPlayable; }
        }

        private Playable m_AnimationClipPlayable;

        public WrapMode wrapMode
        {
            get { return m_ClipPlayableStateWrapMode; }
        }

        private WrapMode m_ClipPlayableStateWrapMode;

        //Clone information
        public bool isClone
        {
            get { return m_IsClone; }
        }

        private bool m_IsClone;

        public bool isReadyForCleanup
        {
            get { return m_ReadyForCleanup; }
        }

        private bool m_ReadyForCleanup;

        public StateHandle parentState
        {
            get { return m_ParentState; }
        }

        /// <summary>
        /// 用于Playable的克隆
        /// </summary>
        public StateHandle m_ParentState;

        public bool enabledDirty { get { return m_EnabledDirty; } }
        public bool weightDirty { get { return m_WeightDirty; } }

        public void ResetDirtyFlags()
        { 
            m_EnabledDirty = false;
            m_WeightDirty = false;
        }

        private bool m_WeightDirty;
        private bool m_EnabledDirty;

        /// <summary>
        /// 是否为最新时间
        /// </summary>
        public void InvalidateTime() { m_TimeIsUpToDate = false; }
        /// <summary>
        /// 是否为最新时间
        /// </summary>
        private bool m_TimeIsUpToDate;
    }





    private StateHandle StateInfoToHandle(StateInfo info)
    {
        return new StateHandle(this, info.index, info.animationClipPlayable);
    }






    private class StateManagement
    {
        /// <summary>
        /// 操作数组只将对应元素置为null，数组长度不变。
        /// </summary>
        private List<StateInfo> m_States;

        public int Count { get { return m_Count; } }

        private int m_Count;

        public StateInfo this[int i]
        {
            get
            {
                return m_States[i];
            }
        }

        public StateManagement()
        {
            m_States = new List<StateInfo>();
        }
        /// <summary>
        /// 插入元素，并设置该StateInfo的index
        /// </summary>
        /// <returns></returns>
        public StateInfo InsertState()
        {
            StateInfo state = new StateInfo();

            int firstAvailable = m_States.FindIndex(s => s == null);
            if (firstAvailable == -1)
            {
                firstAvailable = m_States.Count;
                m_States.Add(state);
            }
            else
            {
                m_States.Insert(firstAvailable, state);
            }

            state.index = firstAvailable;
            m_Count++;
            return state;
        }
        /// <summary>
        /// m_States中至少存在一个 StateInfo 已经准备开播了。  开播前会把enabled置为true 
        /// </summary>
        /// <returns></returns>
        public bool AnyStatePlaying()
        {
            return m_States.FindIndex(s => s != null && s.enabled) != -1;
        }

        /// <summary>
        /// 移出 List<StateInfo>数组。置为null，数组长度不变；并且 通过StateInfo递归清理当前 Playable和output。
        /// </summary>
        /// <param name="index"></param>
        public void RemoveState(int index)
        {
            StateInfo removed = m_States[index];
            m_States[index] = null;
            removed.DestroyPlayable();
            m_Count = m_States.Count;
        }

        public bool RemoveClip(AnimationClip clip)
        {
            bool removed = false;
            for (int i = 0; i < m_States.Count; i++)
            {
                StateInfo state = m_States[i];
                if (state != null &&state.clip == clip)
                {
                    RemoveState(i);
                    removed = true;
                }
            }
            return removed;
        }

        /// <summary>
        /// 遍历 List<StateInfo>，找到名字未name 的StateInfo
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public StateInfo FindState(string name)
        {
            int index = m_States.FindIndex(s => s != null && s.stateName == name);
            if (index == -1)
                return null;

            return m_States[index];
        }

        public void EnableState(int index)
        {
            StateInfo state = m_States[index];
            state.Enable();
        }

        public void DisableState(int index)
        {
            StateInfo state = m_States[index];
            state.Disable();
        }

        public void SetInputWeight(int index, float weight)
        {
            StateInfo state = m_States[index];
            state.SetWeight(weight);
           
        }

        public void SetStateTime(int index, float time)
        {
            StateInfo state = m_States[index];
            state.SetTime(time);
        }

        public float GetStateTime(int index)
        {
            StateInfo state = m_States[index];
            return state.GetTime();
        }

        public bool IsCloneOf(int potentialCloneIndex, int originalIndex)
        {
            StateInfo potentialClone = m_States[potentialCloneIndex];
            return potentialClone.isClone && potentialClone.parentState.index == originalIndex;
        }

        public float GetStateSpeed(int index)
        {
            return m_States[index].speed;
        }
        public void SetStateSpeed(int index, float value)
        {
            m_States[index].speed = value;
        }

        public float GetInputWeight(int index)
        {
            return m_States[index].weight;
        }
        /// <summary>
        /// 动画长度/速度
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public float GetStateLength(int index)
        {
            AnimationClip clip = m_States[index].clip;
            if (clip == null)
                return 0f;
            float speed = m_States[index].speed;
            if (speed == 0f)
                return Mathf.Infinity;

            return clip.length / speed;
        }

        public float GetClipLength(int index)
        {
            AnimationClip clip = m_States[index].clip;
            if (clip == null)
                return 0f;

            return clip.length;
        }
        /// <summary>
        /// playable的持续时间
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public float GetStatePlayableDuration(int index)
        {
            return m_States[index].playableDuration;
        }

        public AnimationClip GetStateClip(int index)
        {
            return m_States[index].clip;
        }

        public WrapMode GetStateWrapMode(int index)
        {
            return m_States[index].wrapMode;
        }

        public string GetStateName(int index)
        {
            return m_States[index].stateName;
        }

        public void SetStateName(int index, string name)
        {
            m_States[index].stateName = name;
        }

        public void StopState(int index, bool cleanup)
        {
            if (cleanup)
            {
                RemoveState(index);
            }
            else
            {
                m_States[index].Stop();
            }
        }

    }



    /// <summary>
    /// 含有两变量：StateHandle state ,float fadeTime;
    /// </summary>
    private struct QueuedState
    {
        public QueuedState(StateHandle s, float t)
        {
            state = s;
            fadeTime = t;
        }

        public StateHandle state;
        public float fadeTime;
    }

}
