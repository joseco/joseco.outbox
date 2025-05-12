using Joseco.Outbox.EFCore.Config;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joseco.Outbox.EFCore.Persistence;

public static class Extensions
{
    public static ModelBuilder AddOutboxModel<E>(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfig<E>());

        return modelBuilder;
    }
}
