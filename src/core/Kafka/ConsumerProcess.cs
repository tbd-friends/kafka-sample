using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using core.Infrastructure;

namespace core.Kafka
{
    public class ConsumerProcess<TKey, TValue>
    {
        private readonly bool _commitOnConsume;
        private readonly ConsumerConfig _configuration;

        public delegate Task MessageConsumedDelegate(Message<TKey, TValue> message);

        public event MessageConsumedDelegate OnMessageConsumed;

        public ConsumerProcess(KafkaOptions options, bool commitOnConsume = false)
        {
            _commitOnConsume = commitOnConsume;
            _configuration = new ConsumerConfig(options.Configuration);
        }

        public Task Consume(string topic, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                var consumer = new ConsumerBuilder<TKey, TValue>(_configuration)
                    .SetValueDeserializer(new JsonDeserializer<TValue>())
                    .Build();

                consumer.Subscribe(topic);

                while (!token.IsCancellationRequested)
                {
                    ConsumeResult<TKey, TValue> consumed = consumer.Consume(TimeSpan.FromMilliseconds(50));

                    if (consumed != null && OnMessageConsumed != null)
                    {
                        await OnMessageConsumed.Invoke(consumed.Message);

                        if (!_commitOnConsume)
                        {
                            consumer.Commit(consumed);
                        }
                    }
                }
            }, token);
        }
    }
}