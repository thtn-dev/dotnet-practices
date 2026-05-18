# Meta Webhook RabbitMQ Super Stream Demo

Demo .NET 10 console apps for publishing and consuming Meta webhook messages with RabbitMQ Stream Protocol and a Super Stream named `meta.webhook.raw`.

## Start RabbitMQ

```bash
docker compose up -d
```

Management UI: <http://localhost:15672>

Credentials:

```text
guest / guest
```

The compose file enables `rabbitmq_stream` and `rabbitmq_stream_management`, and exposes:

- `5672` AMQP, useful for management tooling
- `5552` stream protocol
- `15672` management UI

## Create Super Stream

Create the default 8-partition Super Stream:

```bash
docker compose exec rabbitmq rabbitmq-streams add_super_stream meta.webhook.raw --partitions 8
```

The publisher can also create it:

```bash
dotnet run --project src/MetaWebhookPublisher.Console -- --create-super-stream true --count 1
```

## Run Publisher

Publish from the sample Meta webhook payload:

```bash
dotnet run --project src/MetaWebhookPublisher.Console -- --file sample-meta-webhook-message.json
```

Generate 10000 messages for 100 users:

```bash
dotnet run --project src/MetaWebhookPublisher.Console -- --count 10000 --users 100 --delay-ms 0
```

Useful options:

- `--file sample-meta-webhook-message.json`
- `--count 100`
- `--users 10`
- `--delay-ms 0`
- `--create-super-stream true`

## Run Subscribers

Open 3 terminals:

```bash
dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id sub-1
```

```bash
dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id sub-2
```

```bash
dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id sub-3
```

Useful options:

- `--instance-id sub-1`
- `--consumer-reference webhook-normalizer`
- `--simulate-ms 500`
- `--fail-rate 0.02`
- `--dlq-rate 0.005`
- `--retry-ttl-ms 5000`
- `--max-retry-attempts 3`
- `--source raw`
- `--from first`

`--fail-rate` and `--dlq-rate` are decimals from `0` to `1`, for example `0.1` means 10%.

## Retry TTL And DLQ

The demo uses RabbitMQ Stream Protocol only. Retry and DLQ are separate Super Streams:

- raw: `meta.webhook.raw`
- retry: `meta.webhook.retry`
- DLQ: `meta.webhook.dlq`

Run a raw subscriber. Retryable failures are published to `meta.webhook.retry`, permanent simulated failures are published to `meta.webhook.dlq`.

```bash
dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id raw-1 --source raw --fail-rate 0.05 --dlq-rate 0.01 --retry-ttl-ms 5000 --max-retry-attempts 3
```

Run a retry subscriber in another terminal. It consumes `meta.webhook.retry`, waits until `availableAt`, then processes the message again. If it fails beyond `--max-retry-attempts`, it publishes to `meta.webhook.dlq`.

```bash
dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id retry-1 --source retry --fail-rate 0.05 --dlq-rate 0.01 --retry-ttl-ms 5000 --max-retry-attempts 3
```

For quick DLQ testing, force failures:

```bash
dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id raw-dlq --source raw --fail-rate 0 --dlq-rate 1
```

Consume DLQ messages in another terminal:

```bash
dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id dlq-1 --source dlq --fail-rate 0 --dlq-rate 0
```

For retry exhaustion testing:

```bash
dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id raw-retry --source raw --fail-rate 1 --dlq-rate 0 --retry-ttl-ms 1000 --max-retry-attempts 2
dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id retry-retry --source retry --fail-rate 1 --dlq-rate 0 --retry-ttl-ms 1000 --max-retry-attempts 2
```

This TTL is application-level delay: the retry envelope has `AvailableAt`, and the retry subscriber waits before processing. RabbitMQ Streams do not behave like AMQP TTL/DLX queues, so this keeps the demo in Stream Protocol while still making retry delay observable.

## Expected Behavior

- Publisher serializes `MetaWebhookEvent` and publishes to the Super Stream.
- Routing key is `ConversationId`, built as `meta:{pageId}:{senderId}`.
- The same `conversationId` is consistently routed to the same partition.
- Subscribers use Single Active Consumer with the same consumer reference, `webhook-normalizer`.
- For each partition, only one active consumer instance should process messages for that reference.
- Scale by opening more subscriber processes.
- Maximum useful parallelism is close to the partition count.
- If subscriber count is greater than partition count, some subscribers may stay idle.
- Offsets are stored manually only after successful processing or duplicate detection.
- Duplicate demo detection is in-memory with key `meta:{PageId}:{SenderId}:{MessageId}`.
- Retry/DLQ publishing is confirmed before the source offset is stored, so failed handoff is replayed instead of dropped.

## Configuration

Both apps read optional `appsettings.json` and environment variables with prefix `META_WEBHOOK_`.

Defaults:

```text
RabbitMqHost=127.0.0.1
RabbitMqPort=5552
RabbitMqUser=guest
RabbitMqPassword=guest
SuperStreamName=meta.webhook.raw
RetrySuperStreamName=meta.webhook.retry
DlqSuperStreamName=meta.webhook.dlq
Partitions=8
ConsumerReference=webhook-normalizer
```

Example override:

```bash
META_WEBHOOK_RabbitMqHost=localhost dotnet run --project src/MetaWebhookSubscriber.Console -- --instance-id sub-1
```

## Production Note

This is a console demo. In ASP.NET production, run the subscriber as a dedicated Worker Service or `BackgroundService`, especially when webhook normalization can be slow or high volume. Avoid coupling heavy subscriber workloads directly into the webhook API process unless the workload is small and deliberately bounded.
