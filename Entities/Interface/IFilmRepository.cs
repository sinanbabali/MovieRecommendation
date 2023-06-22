using Core.Dto;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface
{
    public interface IFilmRepository : IGenericRepository<Film>
    {
        List<Film> GetMoviesByPage(int pageNumber, int pageSize);
        void FetchMoviesAndSave(object status);
        void ClearExistingMovies();
        Task SendFilmRecommendationEmail(string senderEmail, string senderName, string recipientEmail, string subject, string content);
        List<Film> GetFilms();
    }
}
