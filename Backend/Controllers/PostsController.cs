
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly int MaxPostsLimit = 50;

        public PostsController(AppDbContext context) {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostID(int id)
        {
            var post = await _context.Posts
                                    .Include(p => p.User)
                                    .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentPosts(
            [FromQuery] int count = 20,
            [FromQuery] int skip = 0
            )
        {
            int limit = Math.Min(count, MaxPostsLimit);

            var posts = await _context.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.Id)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            if (posts == null || !posts.Any())
            {
                return Ok(new List<Posts>());
            }

            return Ok(posts);
        }

        [HttpGet("ByOwner/{ownerId}")]
        public async Task<IActionResult> GetPostsByOwnerId(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                return BadRequest("Owner ID cannot be empty.");
            }

            var posts = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.OwnerID == ownerId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();


            // DACA NU SE GASESC POSTARI, RETURNEZ O LISTA GOALA
            //if (posts == null || !posts.Any()){ 
            //    return new List<Posts>();
            //}

            return Ok(posts);
        }
    }
}
