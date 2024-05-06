// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using UnityEngine;

namespace Animancer.Examples.FineControl
{
    /// <summary>
    /// Demonstrates how to play a single "Wake Up" animation forwards to wake up and backwards to go back to sleep.
    /// </summary>
    /// 
    /// <example><see href="https://kybernetik.com.au/animancer/docs/examples/fine-control/speed-and-time">Speed And Time</see></example>
    /// 
    /// <remarks>
    /// This script is also reused in the 
    /// <see href="https://kybernetik.com.au/animancer/docs/examples/locomotion/directional-blending">
    /// Directional Blending</see> example.
    /// </remarks>
    /// 
    /// https://kybernetik.com.au/animancer/api/Animancer.Examples.FineControl/SpiderBot
    /// 
    [AddComponentMenu(Strings.ExamplesMenuPrefix + "Fine Control - Spider Bot")]
    [HelpURL(Strings.DocsURLs.ExampleAPIDocumentation + nameof(FineControl) + "/" + nameof(SpiderBot))]
    public sealed class SpiderBot : MonoBehaviour
    {
        /************************************************************************************************************************/

        [SerializeField] private AnimancerComponent _Animancer;
        [SerializeField] private ClipTransition _WakeUp;

        /// <summary>p
        /// The [<see cref="SerializeReference"/>] attribute allows any <see cref="ITransition"/> to be assigned to
        /// this field. The <see href="https://kybernetik.com.au/animancer/docs/manual/other/polymorphic">
        /// Polymorphic</see> page explains this system in more detail.
        /// </summary>
        /// <remarks>
        /// The <see href="https://kybernetik.com.au/animancer/docs/examples/fine-control/speed-and-time">
        /// Speed And Time</see> example uses a <see cref="ClipTransition"/> to play a single animation.
        /// <para></para>
        /// The <see href="https://kybernetik.com.au/animancer/docs/examples/locomotion/directional-blending">
        /// Directional Blending</see> example uses a <see cref="MixerTransition2D"/> to blend between various movement
        /// animations.
        /// </remarks>
        [SerializeReference] private ITransition _Move;

        private bool _IsMoving;

        /************************************************************************************************************************/

        // These properties allow the Directional Blending example to access our private fields but not modify them.

        public AnimancerComponent Animancer => _Animancer;

        public ITransition Move => _Move;

        /************************************************************************************************************************/

        private void Awake()
        {
            //不管正放还是倒放，只要NormalizedTime只要超过1或者小于0时。如果动画的 NormalizedTime 依然在往正无穷或者负无穷增减时Events.OnEnd会一直触发.
            _WakeUp.Events.OnEnd = OnWakeUpEnd;

            // Start paused at the beginning of the animation.
            _Animancer.Play(_WakeUp);
            _Animancer.Playable.PauseGraph();

            //这里不采用下面这种形式的原因是：    哪怕他的NormalizedTime==0，她倒放的时间（NormalizedTime）依然会往负无穷大方向增大。
            var state = _Animancer.Play(_WakeUp);
            state.Speed = -1;



            // Normally Unity would evaluate the Playable Graph every frame and apply its output to the model,
            // but that won't happen since it is paused so we manually call Evaluate to make it apply the first frame.
            _Animancer.Evaluate();
        }

        /************************************************************************************************************************/

        private void OnWakeUpEnd()
        {
            if (_WakeUp.State.Speed > 0)
                _Animancer.Play(_Move);
            else
            {
                //Debug.Log("时间结束！！：" + wakeUpState.NormalizedTime);
                _Animancer.Playable.PauseGraph();
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// This script won't actually do anything on its own, it simply exposes this public property for others.
        /// </summary>
        /// <remarks>
        /// The <see href="https://kybernetik.com.au/animancer/docs/examples/fine-control/speed-and-time">
        /// Speed And Time</see> example controls it with a <see cref="UnityEngine.UI.Toggle"/>.
        /// <para></para>
        /// The <see href="https://kybernetik.com.au/animancer/docs/examples/locomotion/directional-blending">
        /// Directional Blending</see> example controls it with a <see cref="Locomotion.SpiderBotAdvanced"/>.
        /// </remarks>
        public bool IsMoving
        {
            get => _IsMoving;
            set
            {
                if (value)
                    WakeUp();
                else
                    GoToSleep();
            }
        }

        /************************************************************************************************************************/

        private void WakeUp()
        {
            if (_IsMoving)
                return;

            _IsMoving = true;

            var state = _Animancer.Play(_WakeUp);
            state.Speed = 1;
            Debug.Log("正向正向正向正向正向正向：" + state.NormalizedTime);
            Debug.Log("正向正向正向正向正向正向：" + state.Weight);
            // Make sure the graph is unpaused (because we pause it when going back to sleep).
            _Animancer.Playable.UnpauseGraph();
        }

        /************************************************************************************************************************/

        private void GoToSleep()
        {
            if (!_IsMoving)
                return;

            _IsMoving = false;

            var state = _Animancer.Play(_WakeUp);
            state.Speed = -1;
            Debug.Log("倒放倒放倒放倒放倒放：" + state.NormalizedTime);
            Debug.Log("倒放倒放倒放倒放倒放：" + state.Weight);

            // If it was past the last frame, skip back to the last frame now that it is playing backwards.
            // Otherwise just play backwards from the current time.
            //因为NormalizedTime是逐渐增加的，所以倒放时，先将NormalizedTime设为1
            if (state.Weight == 0 || state.NormalizedTime > 1)
            {
                state.NormalizedTime = 1;
            }
        }

        /************************************************************************************************************************/


        private void Update()
        {
            //静止时，先播放WakeUp，所以该NormalizedTime逐渐增加，当加到1时，动画播完。开始播放_Move动画，同时权重开始衰减，NormalizedTime任然增加
            Debug.Log("苏醒    时间：" + _Animancer.States[_WakeUp].NormalizedTime);
            Debug.Log("苏醒    权重：" + _Animancer.States[_WakeUp].Weight);
        }
    }
}
