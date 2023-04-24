using Forum.Data;
using Forum.Entities;
using Forum.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Forum.Controllers;

public class ForumController : Controller
{
    private readonly DataContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IPasswordService _passwordService;

    public ForumController(DataContext context, IAuthorizationService authorizationService, IPasswordService passwordService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _passwordService = passwordService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

        // if the database doesn't exist, create one
        await _context.Database.EnsureCreatedAsync();

        // here we want to check if any user has been created. if not, we will set one up. The username and password should be the desired credentials of the user who sets up this forum. TODO: make it so that the username and password variables are stored somewhere like an env file or in appsettings.json.
        if (!await _context.ApplicationUsers.AnyAsync())
        {
            byte[] passwordHash, passwordSalt;
            _passwordService.CreatePasswordHash("pass", out passwordHash, out passwordSalt);

            ApplicationUser user = new ApplicationUser
            {
                Username = "root",
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = "Root",
                CreatedTS = unixTime,
                LastChanged = unixTime,
                ShowModControls = true
            };

            await _context.ApplicationUsers.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        // Getting all categories
        List<Category> categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

        // Iterate over each category
        foreach (Category category in categories)
        {
            // Fill the List<Topic> property for each category
            category.Topics = await _context.Topics.Where(t => t.CategoryId == category.Id && t.IsActive).ToListAsync();

            // Iterate over each element in each categorys List<Topic> property
            foreach (Topic topic in category.Topics)
            {
                Comment mostRecentComment = await _context.Comments.FindAsync(topic.MostRecentCommentId);

                if (mostRecentComment == null)
                {
                    topic.MostRecentCommentedPost = null;
                }
                else
                {
                    mostRecentComment.ApplicationUser = await _context.ApplicationUsers.FindAsync(mostRecentComment.ApplicationUserId);
                    // here do mostRecentComment.HowLongAgo. do calc and string conversion with unixTime variable.

                    Post mostRecentCommentedPost = await _context.Posts.FindAsync(mostRecentComment.PostId);

                    int totalCommentsInPost = await _context.Comments.Where(c => c.PostId == mostRecentCommentedPost.Id && c.IsActive).CountAsync();

                    mostRecentCommentedPost.LastPage = int.Parse(Math.Ceiling(((decimal)totalCommentsInPost / 2)).ToString());
                    mostRecentCommentedPost.MostRecentComment = mostRecentComment;
                    
                    topic.MostRecentCommentedPost = mostRecentCommentedPost;
                }
            }
        }

        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Topic(int id, int? page)
    {
        Topic topic = await _context.Topics.FindAsync(id);

        if (topic == null)
        {
            return View(null);
        }

        int pageIndex = page - 1 ?? 0;

        List<Post> posts = await _context.Posts.Where(p => p.TopicId == id && p.IsActive)
        .OrderByDescending(p => p.MostRecentCommentTS)
        .Skip(2 * pageIndex)
        .Take(2)
        .ToListAsync();

        if (posts == null)
        {
            topic.Posts = null;
        }
        else
        {
            foreach (Post post in posts)
            {
                Comment mostRecentComment = await _context.Comments.FindAsync(post.MostRecentCommentId);

                if (mostRecentComment == null)
                {
                    post.MostRecentComment = null;
                }
                else
                {
                    mostRecentComment.ApplicationUser = await _context.ApplicationUsers.FindAsync(mostRecentComment.ApplicationUserId);
                    // like in the other action, work out howlongago.
                    
                    int totalCommentsInPost = await _context.Comments.Where(c => c.PostId == post.Id && c.IsActive).CountAsync();

                    post.LastPage = int.Parse(Math.Ceiling(((decimal)totalCommentsInPost / 2)).ToString());
                    post.MostRecentComment = mostRecentComment;
                }
            }

            topic.Posts = posts;
        }

        int totalItems = await _context.Posts.Where(p => p.TopicId == id && p.IsActive).CountAsync();

        ViewBag.totalItems = totalItems;
        ViewBag.itemsPerPage = 2;
        ViewBag.currentPage = pageIndex + 1;
        ViewBag.totalPages = int.Parse(Math.Ceiling(((decimal)totalItems / 2)).ToString());
        ViewBag.previous = pageIndex;
        ViewBag.next = pageIndex + 2;

        return View(topic);
    }

    [HttpGet]
    public async Task<IActionResult> Post(int id, int? page)
    {
        Post post = await _context.Posts.Where(p => p.Id == id && p.IsActive).FirstOrDefaultAsync();

        if (post == null)
        {
            return View(null);
        }

        post.ApplicationUser = await _context.ApplicationUsers.FindAsync(post.ApplicationUserId);
        post.Topic = await _context.Topics.FindAsync(post.TopicId);

        int pageIndex = page - 1 ?? 0;

        List<Comment> comments = await _context.Comments.Where(c => c.PostId == id && c.IsActive)
        .OrderBy(c => c.CreatedTS)
        .Skip(2 * pageIndex)
        .Take(2)
        .ToListAsync();

        if (comments == null)
        {
            post.Comments = null;
        }
        else
        {
            foreach (Comment comment in comments)
            {
                comment.ApplicationUser = await _context.ApplicationUsers.FindAsync(comment.ApplicationUserId);
                // work out howlongago.
            }

            post.Comments = comments;
        }

        int totalItems = await _context.Comments.Where(c => c.PostId == id && c.IsActive).CountAsync();

        ViewBag.totalItems = totalItems;
        ViewBag.itemsPerPage = 2;
        ViewBag.currentPage = pageIndex + 1;
        ViewBag.totalPages = int.Parse(Math.Ceiling(((decimal)totalItems / 2)).ToString());
        ViewBag.previous = pageIndex;
        ViewBag.next = pageIndex + 2;

        return View(post);
    }

    [HttpGet]
    [Authorize(Policy = "IsRootOrAdmin")]
    public async Task<IActionResult> AddCategory()
    {
        // database, get number of categories, in this example we just pass the number 3

        return View(3);
    }

    [HttpPost]
    [Authorize(Policy = "IsRootOrAdmin")]
    public async Task<IActionResult> AddCategory(string categoryName)
    {
        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

        Category category = new Category
        {
            Name = categoryName,
            IsActive = true,
            CreatedTS = unixTime
        };

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Forum");
    }

    [HttpGet]
    [Authorize(Policy = "IsRootOrAdmin")]
    public async Task<IActionResult> AddTopic(int id)
    {
        Category category = await _context.Categories.FindAsync(id);
        // TODO: all checks, active, etc.

        return View(category);
    }

    [HttpPost]
    [Authorize(Policy = "IsRootOrAdmin")]
    public async Task<IActionResult> AddTopic(int categoryId, string topicName)
    {
        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

        Topic topic = new Topic
        {
            Name = topicName,
            CategoryId = categoryId,
            IsActive = true,
            CreatedTS = unixTime,
            MostRecentCommentId = null
        };

        await _context.Topics.AddAsync(topic);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Forum");
    }

    [HttpGet]
    public async Task<IActionResult> AddPost(int id)
    {
        Topic topic = await _context.Topics.Where(t => t.Id == id && t.IsActive).FirstOrDefaultAsync();

        if (topic == null)
        {
            return new ForbidResult();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, topic.CategoryId, "IsValidUser");

        if (!authorizationResult.Succeeded)
        {
            return new ForbidResult();
        }

        return View(topic);
    }

    [HttpPost]
    public async Task<IActionResult> AddPost(int topicId, string postTitle, string commentContent)
    {
        Topic topic = await _context.Topics.Where(t => t.Id == topicId && t.IsActive).FirstOrDefaultAsync();

        if (topic == null)
        {
            return new ForbidResult();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, topic.CategoryId, "IsValidUser");

        if (!authorizationResult.Succeeded)
        {
            return new ForbidResult();
        }

        var userIdString = User.Claims.FirstOrDefault(u => u.Type == "UserId").Value;
        int userId = Int32.Parse(userIdString);

        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

        Post post = new Post
        {
            Title = postTitle,
            TopicId = topicId,
            CategoryId = topic.CategoryId,
            IsActive = true,
            CreatedTS = unixTime,
            ApplicationUserId = userId
        };

        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();

        Comment comment = new Comment
        {
            Content = commentContent,
            PostId = post.Id,
            TopicId = topicId,
            CategoryId = topic.CategoryId,
            IsActive = true,
            CreatedTS = unixTime,
            ApplicationUserId = userId
        };

        await _context.Comments.AddAsync(comment);
        await _context.SaveChangesAsync();

        post.FirstCommentId = comment.Id;
        post.MostRecentCommentId = comment.Id;
        post.MostRecentCommentTS = unixTime;

        topic.MostRecentCommentId = comment.Id;
        
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Forum");
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(int postId, string content)
    {
        Post post = await _context.Posts.Where(p => p.Id == postId && p.IsActive).FirstOrDefaultAsync();

        if (post == null)
        {
            return new ForbidResult();
        }

        Topic topic = await _context.Topics.Where(t => t.Id == post.TopicId && t.IsActive).FirstOrDefaultAsync();

        if (topic == null)
        {
            return new ForbidResult();
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, post.CategoryId, "IsValidUser");

        if (!authorizationResult.Succeeded)
        {
            return new ForbidResult();
        }

        var userIdString = User.Claims.FirstOrDefault(u => u.Type == "UserId").Value;
        int userId = Int32.Parse(userIdString);

        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

        Comment comment = new Comment
        {
            Content = content,
            PostId = postId,
            TopicId = topic.Id,
            CategoryId = topic.CategoryId,
            IsActive = true,
            CreatedTS = unixTime,
            ApplicationUserId = userId
        };

        await _context.Comments.AddAsync(comment);
        await _context.SaveChangesAsync();

        post.MostRecentCommentId = comment.Id;
        post.MostRecentCommentTS = unixTime;

        topic.MostRecentCommentId = comment.Id;
        
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Forum");
    }

    [HttpPost]
    public async Task<IActionResult> EditComment(int editCommentId, string editCommentContent, int currentPage)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, editCommentId, "EditComment")).Succeeded)
        {
            return new ForbidResult();
        }
        else
        {
            Comment comment = await _context.Comments.FindAsync(editCommentId);
            comment.Content = editCommentContent;

            await _context.SaveChangesAsync();

            return RedirectToAction("Post", "Forum", new { id = comment.PostId, page = currentPage });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteComment(int deleteCommentId, int currentPage)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, deleteCommentId, "DeleteComment")).Succeeded)
        {
            return new ForbidResult();
        }
        else
        {
            Comment comment = await _context.Comments.FindAsync(deleteCommentId);
            comment.IsActive = false;

            await _context.SaveChangesAsync();

            Post post = await _context.Posts.FindAsync(comment.PostId);

            Comment newMostRecentCommentForPost = await _context.Comments.Where(c => c.PostId == post.Id && c.IsActive)
            .OrderByDescending(c => c.CreatedTS)
            .FirstOrDefaultAsync();
            
            if (newMostRecentCommentForPost == null)
            {
                post.MostRecentCommentId = null;
                post.MostRecentCommentTS = null;
            }
            else
            {
                post.MostRecentCommentId = newMostRecentCommentForPost.Id;
                post.MostRecentCommentTS = newMostRecentCommentForPost.CreatedTS;
            }

            Topic topic = await _context.Topics.FindAsync(comment.TopicId);

            var query = from c in _context.Comments
                        join p in _context.Posts
                        on c.PostId equals p.Id
                        where p.TopicId == topic.Id && c.IsActive && p.IsActive
                        orderby c.CreatedTS descending
                        select c;

            Comment newMostRecentCommentForTopic = await query.FirstOrDefaultAsync();
            
            if (newMostRecentCommentForTopic == null)
            {
                topic.MostRecentCommentId = null;
            }
            else
            {
                topic.MostRecentCommentId = newMostRecentCommentForTopic.Id;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Post", "Forum", new { id = comment.PostId, page = currentPage });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeletePost(int deletePostId, int currentPage)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, deletePostId, "DeletePost")).Succeeded)
        {
            return new ForbidResult();
        }
        else
        {
            Post post = await _context.Posts.FindAsync(deletePostId);
            post.IsActive = false;

            await _context.SaveChangesAsync();

            Topic topic = await _context.Topics.FindAsync(post.TopicId);

            var query = from c in _context.Comments
                        join p in _context.Posts
                        on c.PostId equals p.Id
                        where p.TopicId == topic.Id && c.IsActive && p.IsActive
                        orderby c.CreatedTS descending
                        select c;

            Comment newMostRecentCommentForTopic = await query.FirstOrDefaultAsync();
            
            if (newMostRecentCommentForTopic == null)
            {
                topic.MostRecentCommentId = null;
            }
            else
            {
                topic.MostRecentCommentId = newMostRecentCommentForTopic.Id;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Topic", "Forum", new { id = post.TopicId, page = currentPage });
        }
    }

    [HttpPost]
    [Authorize(Policy = "IsRootOrAdmin")]
    public async Task<IActionResult> DeleteTopic(int deleteTopicId)
    {
        Topic topic = await _context.Topics.FindAsync(deleteTopicId);
        topic.IsActive = false;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Forum");
    }

    [HttpPost]
    [Authorize(Policy = "IsRootOrAdmin")]
    public async Task<IActionResult> DeleteCategory(int deleteCategoryId)
    {
        Category category = await _context.Categories.FindAsync(deleteCategoryId);
        category.IsActive = false;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Forum");
    }

    [HttpGet]
    public async Task<IActionResult> UserInformation(int id)
    {
        ApplicationUser applicationUser = await _context.ApplicationUsers.Where(u => u.Id == id).FirstOrDefaultAsync();

        return View(applicationUser);
    }
}