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
        /// Kullanýcý kimlik doðrulama iþlemini gerçekleþtirmek için kullanýlýr.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("Authenticate")]
        [Description("<b>Aktif Kullanýcýlar</b> <br> username:Sinan password:deneme123 role:<b>admin</b> <br> username:Mikro password:test123 role:<b>guest</b>")]
        public ServiceResponse Authenticate([FromBody] AuthenticateRequest model)
        {
            var token = _userService.Login(model.Username, model.Password);

            if (token == null || token == String.Empty)
                return ServiceResponse.Fail("Kullanýcý adý veya þifre yanlýþ");

            return ServiceResponse.Success(token);
        }

        /// <summary>
        /// Belirli bir süreçte filmlerin alýnmasýný ve veritabanýna kaydedilmesini baþlatýr
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
        /// Filmlerin alýnmasýný sonlandýrýr
        /// </summary>
        [Authorize]
        [HttpGet("StopFetchingMovies")]
        //[Description("Filmlerin alýnmasýný <b>sonlandýrýr</b>")]
        public ActionResult StopFetchingMovies()
        {
            timer.Dispose();
            return Ok("Movie fetching stopped.");
        }

        /// <summary>
        /// Film listesi belirtilen sayfa numarasý ve sayfa boyutuna göre döndürülür.
        /// </summary>
        /// <param name="pageNumber">Sayfa numarasý</param>
        /// <param name="pageSize">Sayfa baþýna gösterilecek film sayýsý</param>
        [Authorize]
        [HttpGet("GetMoviesByPage")]
        public ServiceResponse GetMoviesByPageGetMoviesByPage(
                  [Description("Sayfa numarasý")] int pageNumber,
                  [Description("Sayfa baþýna gösterilecek film sayýsý")] int pageSize)
        {
            try
            {
                var Films = _unitOfWork.Films.GetMoviesByPage(pageNumber, pageSize);
                if (Films.Count == 0)
                {
                    return ServiceResponse.Success($"Getirilecek Film Bulunamadý - {DateTime.Now}", Films);

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
        /// Belirtilen ID'ye sahip bir filmin detaylarýný getirir.
        /// </summary>
        /// <param name="id">Film ID'si</param>
        [Authorize]
        [HttpGet("GetFilm")]
        public ServiceResponse GetFilmDetail(int id)
        {
            var Film = _unitOfWork.Films.GetFilms().FirstOrDefault(x => x.Id == id);
            if (Film is null) return ServiceResponse.Fail($"{id} Id Numarasýna Sahip Film Bulunamadý");

            return ServiceResponse.Success($"Film Bilgisi Getirildi- {DateTime.Now}", Film);
        }

        /// <summary>
        /// Belirtilen filmi günceller.
        /// </summary>
        /// <param name="id">Film ID'si</param>
        /// <param name="note">Film notu</param>
        /// <param name="point">Film puaný</param>
        [Authorize]
        [HttpGet("UpdateFilm")]
        public ServiceResponse UpdateFilm(int id, string note, int point)
        {
            var Film = _unitOfWork.Films.GetFilms().FirstOrDefault(x => x.Id == id);

            if (Film is null) return ServiceResponse.Fail($"{id} Id Numarasýna Sahip Film Bulunamadý");

            if (!new ValidationHelper().IsValidRating(point, 1, 10)) return ServiceResponse.Fail($"Puan 1-10 Arasý Bir Tam Sayý Olmak Zorundadýr.");

            Film.Note = note;
            Film.Point = point;

            _unitOfWork.Films.Update(Film);
            _unitOfWork.SaveChanges();

            return ServiceResponse.Success($"Film Güncellendi Getirildi- {DateTime.Now}", Film);
        }

        /// <summary>
        /// Kullanýcýya film önerisi içeren bir e-posta gönderir.
        /// </summary>
        /// <param name="id">Film ID'si</param>
        /// <param name="email">Kullanýcýnýn e-posta adresi</param>
        [Authorize]
        [HttpGet("SendRecommendationEmail")]
        public async Task<ServiceResponse> SendRecommendationEmail(int id, string email)
        {
            var Film = _unitOfWork.Films.GetFilms().FirstOrDefault(x => x.Id == id);
            if (Film is null) return ServiceResponse.Fail($"{id} Id Numarasýna Sahip Film Bulunamadý");

            var Subject = $"Film Tavsiyesi - Ýyi Seyirler! - {DateTime.Now}";
            var Content = $"Merhaba, \n Sana Tavsiye ettiðim filmi umarým beðenirsin - Film Adý : {Film.Name} , Genel Bakýþ : {Film.Overview}";

            try
            {
                await _unitOfWork.Films.SendFilmRecommendationEmail("sinannbabali@gmail.com", "Sinan Babalý", email, Subject, Content);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Fail(ex.Message);
            }

            return ServiceResponse.Success($"Film tavsiyesi e-postasý gönderildi. Alýcý: {email}, Film: {Film.Name}");
        }

        /// <summary>
        /// Test Film eklemek için kullanýlýr. Sadece "admin" rolüne sahip kullanýcýlar eriþebilir.
        /// </summary>
        [Authorize(Roles = "admin")]
        [HttpGet("AddTestMovies")]
        public ServiceResponse AddTestMovies()
        {
            Film Film = new Film
            {
                Name = "Test 1",
                OriginalLanguage = "türkçe",
                Overview = "açýklamasý da test",
                ReleaseDate = DateTime.Now
            };

            _unitOfWork.Films.Add(Film);
            _unitOfWork.SaveChanges();

            var Films = _unitOfWork.Films.GetFilms();

            object ResultData = new { TotalCount = Films.Count, LatestMovies = Films.OrderByDescending(x => x.Id).Take(10).ToList()};

            return ServiceResponse.Success($"Film Baþarýyla Eklendi - {DateTime.Now}", ResultData);
        }
}
}