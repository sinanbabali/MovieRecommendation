using Core.Dto;
using Core.Interface;
using Core.Repositories;
using Entities;
using Entities.Models;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Utilities.Helpers;
using Utilities.Http;


namespace FilmifyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private static Timer timer;

        private readonly IUnitOfWork _unitOfWork;
        private IUserService _userService;
        public MovieController(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        /// <summary> 
        /// Kullan�c� kimlik do�rulama i�lemini ger�ekle�tirmek i�in kullan�l�r.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("Authenticate")]
        [Description("<b>Aktif Kullan�c�lar</b> <br> username:Sinan password:deneme123 role:<b>admin</b> <br> username:Mikro password:test123 role:<b>guest</b>")]
        public ServiceResponse Authenticate([FromBody] AuthenticateRequest model)
        {
            var token = _userService.Login(model.Username, model.Password);

            if (token == null || token == String.Empty)
                return ServiceResponse.Fail("Kullan�c� ad� veya �ifre yanl��");

            return ServiceResponse.Success(token);
        }

        /// <summary>
        /// Belirli bir s�re�te filmlerin al�nmas�n� ve veritaban�na kaydedilmesini ba�lat�r
        /// </summary>
        [Authorize]
        [HttpGet("StartFetchingMovies")]
        public ActionResult StartFetchingMovies()
        {
            _unitOfWork.Films.ClearExistingMovies();
            timer = new Timer(_unitOfWork.Films.FetchMoviesAndSave, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
            return Ok("Movie fetching started.");
        }

        /// <summary>
        /// Filmlerin al�nmas�n� sonland�r�r
        /// </summary>
        [Authorize]
        [HttpGet("StopFetchingMovies")]
        //[Description("Filmlerin al�nmas�n� <b>sonland�r�r</b>")]
        public ActionResult StopFetchingMovies()
        {
            timer.Dispose();
            return Ok("Movie fetching stopped.");
        }

        /// <summary>
        /// Film listesi belirtilen sayfa numaras� ve sayfa boyutuna g�re d�nd�r�l�r.
        /// </summary>
        /// <param name="pageNumber">Sayfa numaras�</param>
        /// <param name="pageSize">Sayfa ba��na g�sterilecek film say�s�</param>
        [Authorize]
        [HttpGet("GetMoviesByPage")]
        public ServiceResponse GetMoviesByPageGetMoviesByPage(
                  [Description("Sayfa numaras�")] int pageNumber,
                  [Description("Sayfa ba��na g�sterilecek film say�s�")] int pageSize)
        {
            try
            {
                var Films = _unitOfWork.Films.GetMoviesByPage(pageNumber, pageSize);
                if (Films.Count == 0)
                {
                    return ServiceResponse.Success($"Getirilecek Film Bulunamad� - {DateTime.Now}", Films);

                }
                else
                {
                    return ServiceResponse.Success($"Filmler Getirildi- {DateTime.Now}", Films);
                }
            }
            catch (Exception ex)
            {
                return ServiceResponse.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Belirtilen ID'ye sahip bir filmin detaylar�n� getirir.
        /// </summary>
        /// <param name="id">Film ID'si</param>
        [Authorize]
        [HttpGet("GetFilm")]
        public ServiceResponse GetFilmDetail(int id)
        {
            var Film = _unitOfWork.Films.GetFilms().FirstOrDefault(x => x.Id == id);
            if (Film is null) return ServiceResponse.Fail($"{id} Id Numaras�na Sahip Film Bulunamad�");

            return ServiceResponse.Success($"Film Bilgisi Getirildi- {DateTime.Now}", Film);
        }

        /// <summary>
        /// Belirtilen filmi g�nceller.
        /// </summary>
        /// <param name="id">Film ID'si</param>
        /// <param name="note">Film notu</param>
        /// <param name="point">Film puan�</param>
        [Authorize]
        [HttpGet("UpdateFilm")]
        public ServiceResponse UpdateFilm(int id, string note, int point)
        {
            var Film = _unitOfWork.Films.GetFilms().FirstOrDefault(x => x.Id == id);

            if (Film is null) return ServiceResponse.Fail($"{id} Id Numaras�na Sahip Film Bulunamad�");

            if (!new ValidationHelper().IsValidRating(point, 1, 10)) return ServiceResponse.Fail($"Puan 1-10 Aras� Bir Tam Say� Olmak Zorundad�r.");

            Film.Note = note;
            Film.Point = point;

            _unitOfWork.Films.Update(Film);
            _unitOfWork.SaveChanges();

            return ServiceResponse.Success($"Film G�ncellendi Getirildi- {DateTime.Now}", Film);
        }

        /// <summary>
        /// Kullan�c�ya film �nerisi i�eren bir e-posta g�nderir.
        /// </summary>
        /// <param name="id">Film ID'si</param>
        /// <param name="email">Kullan�c�n�n e-posta adresi</param>
        [Authorize]
        [HttpGet("SendRecommendationEmail")]
        public async Task<ServiceResponse> SendRecommendationEmail(int id, string email)
        {
            var Film = _unitOfWork.Films.GetFilms().FirstOrDefault(x => x.Id == id);
            if (Film is null) return ServiceResponse.Fail($"{id} Id Numaras�na Sahip Film Bulunamad�");

            var Subject = $"Film Tavsiyesi - �yi Seyirler! - {DateTime.Now}";
            var Content = $"Merhaba, \n Sana Tavsiye etti�im filmi umar�m be�enirsin - Film Ad� : {Film.Name} , Genel Bak�� : {Film.Overview}";

            try
            {
                await _unitOfWork.Films.SendFilmRecommendationEmail("sinannbabali@gmail.com", "Sinan Babal�", email, Subject, Content);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Fail(ex.Message);
            }

            return ServiceResponse.Success($"Film tavsiyesi e-postas� g�nderildi. Al�c�: {email}, Film: {Film.Name}");
        }

        /// <summary>
        /// Test Film eklemek i�in kullan�l�r. Sadece "admin" rol�ne sahip kullan�c�lar eri�ebilir.
        /// </summary>
        [Authorize(Roles = "admin")]
        [HttpGet("AddTestMovies")]
        public ServiceResponse AddTestMovies()
        {
            Film Film = new Film
            {
                Name = "Test 1",
                OriginalLanguage = "t�rk�e",
                Overview = "a��klamas� da test",
                ReleaseDate = DateTime.Now
            };

            _unitOfWork.Films.Add(Film);
            _unitOfWork.SaveChanges();

            var Films = _unitOfWork.Films.GetFilms();

            object ResultData = new { TotalCount = Films.Count, LatestMovies = Films.OrderByDescending(x => x.Id).Take(10).ToList()};

            return ServiceResponse.Success($"Film Ba�ar�yla Eklendi - {DateTime.Now}", ResultData);
        }
}
}