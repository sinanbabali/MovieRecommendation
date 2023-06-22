using Core.Interface;
using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MovieContext _dbContext;

        public UnitOfWork(MovieContext dbContext)
        {
            _dbContext = dbContext;
            Films = new FilmRepository(dbContext);
        }

        public IFilmRepository Films { get; private set; }


        public void SaveChanges()
            => _dbContext.SaveChanges();


        public async Task SaveChangesAsync()
            => await _dbContext.SaveChangesAsync();


        public void Rollback()
            => _dbContext.Dispose();


        public async Task RollbackAsync()
            => await _dbContext.DisposeAsync();
    }
}
