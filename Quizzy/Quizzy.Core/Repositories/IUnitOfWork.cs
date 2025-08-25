using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Repositories
{
    public interface IUnitOfWork
    {

        public Task SaveChangesAsync(CancellationToken cancellationToken = default);

    }
}
