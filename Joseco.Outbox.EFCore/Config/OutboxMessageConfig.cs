using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Joseco.Outbox.Contracts.Model;

namespace Joseco.Outbox.EFCore.Config;

internal class OutboxMessageConfig<E>(string schema = "outbox") : IEntityTypeConfiguration<OutboxMessage<E>>
{
    public void Configure(EntityTypeBuilder<OutboxMessage<E>> builder)
    {
        builder.ToTable("outboxMessage", schema);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("outboxId");

        builder.Property(x => x.Created)
            .HasColumnName("created");

        builder.Property(x => x.Type)
            .HasColumnName("type");

        builder.Property(x => x.Processed)
            .HasColumnName("processed");

        builder.Property(x => x.ProcessedOn)
            .HasColumnName("processedOn");

        var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        var contentConverter = new ValueConverter<E, string>(
           obj => JsonConvert.SerializeObject(obj, jsonSettings),
           stringValue => JsonConvert.DeserializeObject<E>(stringValue, jsonSettings)
       );

        builder.Property(x => x.Content)
            .HasConversion(contentConverter)
            .HasColumnName("content");

    }
}
