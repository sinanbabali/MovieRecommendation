using Core.Dto;
using Core.Interface;
using Entities;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Utilities.Http;
using static Utilities.Helpers.EmailService;

namespace Core.Repositories
{
    public class FilmRepository : GenericRepository<Film>, IFilmRepository
    {
        public FilmRepository(MovieContext context) : base(context)
        {
        }

        public List<Film> GetMoviesByPage(int pageNumber, int pageSize)
        {
            var Films = GetFilms();

            int TotalPageCount = (int)Math.Ceiling((double)Films.Count / pageSize);
            if (pageNumber < 1 || pageNumber > TotalPageCount)
            {
                throw new ArgumentException($"Sayfa Numarası Geçersiz : {pageNumber}");
            }

            int StartIndex = (pageNumber - 1) * pageSize;
            int EndIndex = Math.Min(StartIndex + pageSize, Films.Count);

            List<Film> PagedRecords = Films.GetRange(StartIndex, EndIndex - StartIndex).OrderByDescending(x => x.Id).ToList();

            return PagedRecords;
        }

        public async Task<List<Movie>> FetchDataFromApi()
        {
            string RequestUrl = "https://api.themoviedb.org/3/trending/movie/day?language=tr-TR";
            ClientHelper client = new ClientHelper();

            List<Movie> Movies = new List<Movie>();

            Dictionary<string, string> Headers = new Dictionary<string, string>
            {
                 { "accept", "application/json" },
                 { "Authorization", "Bearer eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiJmM2Y5YmE1YWNiNDFkYzZmYjA1MmVkOTI0MDJhOGEyZSIsInN1YiI6IjY0OGM0NWY5NTU5ZDIyMDBjNTc1YTZjMyIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.BPDSzX2mOa1BEdV5y-u8bcU-XNRc9k_a0I4qv5Zh5i0" }
            };

            TheMovie TheMovie = await client.GetAsync<TheMovie>(RequestUrl, Headers);
            long PageCount = Math.Min(TheMovie.TotalPages, 500); //501 gönderdiğimde Invalid page: Pages start at 1 and max at 500. They are expected to be an integer.
                                                                 //hatası vericek şekilde yazılmış api , bu yüzden minumum=> page kadar , maksimum 500 olacak şekilde istek atmak istedim

            //PageCount = 1;

            for (int page = 1; page <= PageCount; page++)
            {
                string GetRequestUrl = $"{RequestUrl}&page={page}";
                TheMovie = await client.GetAsync<TheMovie>(GetRequestUrl, Headers);

                Movies.AddRange(TheMovie.Movies);
            }

            return Movies;
        }

        public void FetchMoviesAndSave(object status)
        {
            var Movies = FetchDataFromApi().Result;

            List<Film> AddMovies = new List<Film>();


            try
            {
                foreach (var item in Movies)
                {
                    Film Film = new Film
                    {
                        Name = item.Title,
                        OriginalLanguage = item.OriginalLanguage,
                        Overview = item.Overview,
                        ReleaseDate = item.ReleaseDate ?? DateTimeOffset.MinValue
                };

                    AddMovies.Add(Film);
                }

                using (var context = new MovieContext())
                {
                    context.Films.AddRange(AddMovies);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {

                throw;
            }


            
        }

        public async Task SendFilmRecommendationEmail(string senderEmail, string senderName, string recipientEmail, string subject, string content)
        {
            var RequestUrl = "https://api.sendinblue.com/v3/smtp/email";
            var apiKey = "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";

            ClientHelper client = new ClientHelper();

            var Headers = new Dictionary<string, string>
            {
                { "accept", "application/json" },
                { "api-key", apiKey }
            };

            var data = new EmailData
            {
                Sender = new SenderInfo
                {
                    Name = senderName,
                    Email = senderEmail
                },
                To = new[] { new RecipientInfo { Email = recipientEmail } },
                Subject = subject,
                HtmlContent = content,
            };

            var response = await client.PostAsync(RequestUrl, data, Headers);

            if (!response.Success)
            {
                // E-posta gönderimi başarısız oldu
                throw new Exception($"E-posta gönderimi başarısız oldu. Hata Mesajı: {response.Response}");
            }
        }

        public void ClearExistingMovies()
        {
            using (var context = new MovieContext())
            {
                var Films = context.Films.ToList();
                if (Films.Count > 0)
                {
                    context.RemoveRange(Films);
                    context.SaveChangesAsync();
                }
            }
        }

        public List<Film> GetFilms()
        {
            return _context.Films.ToList();
        }
    }
}
