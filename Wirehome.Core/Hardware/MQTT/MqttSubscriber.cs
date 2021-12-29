﻿using System;
using MQTTnet;
using MQTTnet.Server.Internal;

namespace Wirehome.Core.Hardware.MQTT
{
    public sealed class MqttSubscriber
    {
        readonly Action<MqttApplicationMessageReceivedEventArgs> _callback;

        public MqttSubscriber(string uid, string topicFilter, Action<MqttApplicationMessageReceivedEventArgs> callback)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public string TopicFilter { get; }

        public string Uid { get; }

        public bool IsFilterMatch(string topic)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            return MqttTopicFilterComparer.IsMatch(topic, TopicFilter);
        }

        public void Notify(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            _callback(eventArgs);
        }
    }
}