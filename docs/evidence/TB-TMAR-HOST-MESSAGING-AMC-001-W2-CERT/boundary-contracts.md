# boundary-contracts — TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT

- Persistence/serializer: IIntegrationEventSerializer, IDatabaseConnectionResolver, ConnectionReference only
- Transport: Tooba.Host.Transport envelope/consumer/mapper only; Transport AMC not opened
- Outbox: owns poll/delivery; Messaging is publish adapter only
- Health: MessagingHostOptions + IBusControl; HOST_HEALTH_AMC_CERTIFIED preserved
- Foreign App/Infra/Domain/DbContext: ZERO
- Business authority: ZERO
- Message-text classification: ZERO
- User-facing runtime presentation text: ZERO
- Sensitive leakage: ZERO
