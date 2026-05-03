using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryDomain.Model;
using LibraryInfrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(DbLibraryContext context)
    {
        await EnsureStatusesAsync(context);
        await EnsureRatingsAsync(context);
        await EnsureTagsAsync(context);

        _ = await EnsureUserAsync(context, "admin", "admin@admin.local", "password123", "Керую системою та модерую контент.", AppRoles.Admin);
        var author = await EnsureUserAsync(context, "demo_author", "author@demo.local", "password123", "Пишу романтичні та пригодницькі фанфіки.", AppRoles.Author);
        var reader = await EnsureUserAsync(context, "demo_reader", "reader@demo.local", "password123", "Люблю читати і коментувати.", AppRoles.Reader);
        var reader2 = await EnsureUserAsync(context, "genre_lover", "genres@demo.local", "password123", "Читаю все: від драми до гумору.", AppRoles.Reader);

        var draftStatusId = await context.FanficStatuses.AsNoTracking().Where(s => s.Name == "Draft").Select(s => s.Id).FirstAsync();
        var teenRatingId = await context.ContentRatings.AsNoTracking().Where(r => r.Name == "Teen").Select(r => r.Id).FirstAsync();
        var matureRatingId = await context.ContentRatings.AsNoTracking().Where(r => r.Name == "Mature").Select(r => r.Id).FirstAsync();
        var generalRatingId = await context.ContentRatings.AsNoTracking().Where(r => r.Name == "General").Select(r => r.Id).FirstAsync();

        var work1Id = await EnsureWorkAsync(
            context,
            author.Id,
            "Листи крізь бурю",
            "Романтика і дружба після штормової ночі, коли старі листи змінюють долю героїв.",
            teenRatingId,
            draftStatusId,
            new[] { "romance", "friendship", "drama" },
            new[]
            {
                (1, "Початок дощу", "Коли грім розрізав небо, Марта знайшла лист, адресований їй ще десять років тому..."),
                (2, "Старі адреси", "Лист вів у покинутий будинок, де на стінах були мапи з дивними відмітками.")
            });

        var work2Id = await EnsureWorkAsync(
            context,
            author.Id,
            "Вогонь і кришталь",
            "Фентезі-ангст про суперників, які змушені об'єднатися.",
            matureRatingId,
            draftStatusId,
            new[] { "fantasy", "angst", "adventure" },
            new[]
            {
                (1, "Попіл у вітрі", "Арка магії тріснула, і кришталевий вогонь накрив площу..."),
                (2, "Клятва гільдії", "Ейлін і Каель підписали клятву, що або врятують місто, або загинуть разом.")
            });

        var work3Id = await EnsureWorkAsync(
            context,
            author.Id,
            "Кава, кіт і дедлайн",
            "Легка гумористична історія про письменницю, редактора і кота, що постійно ламає плани.",
            generalRatingId,
            draftStatusId,
            new[] { "humor", "friendship" },
            new[]
            {
                (1, "Понеділок починається з лап", "Кіт скинув рукопис із столу рівно за п'ять хвилин до дедлайну."),
                (2, "Редактор у паніці", "Редактор пише десяте повідомлення, а героїня шукає кішку під диваном.")
            });

        await EnsureFanficCommentAsync(context, work1Id, reader.Id, "Дуже атмосферно, подобається стиль!");
        await EnsureFanficCommentAsync(context, work2Id, reader2.Id, "Сильна напруга між героями, чекаю продовження.");
        await EnsureFanficCommentAsync(context, work3Id, reader.Id, "Нарешті щось смішне, це топ.");

        var work1Chapter1Id = await context.Chapters.AsNoTracking().Where(c => c.FanficId == work1Id && c.ChapterNumber == 1).Select(c => c.Id).FirstAsync();
        var work2Chapter1Id = await context.Chapters.AsNoTracking().Where(c => c.FanficId == work2Id && c.ChapterNumber == 1).Select(c => c.Id).FirstAsync();

        await EnsureChapterCommentAsync(context, work1Id, work1Chapter1Id, reader2.Id, "Опис дощу просто ідеальний.");
        await EnsureChapterCommentAsync(context, work2Id, work2Chapter1Id, reader.Id, "Цей початок заслуговує серіалізації.");

        await EnsureLikeAsync(context, reader.Id, work1Id);
        await EnsureLikeAsync(context, reader2.Id, work2Id);
        await EnsureBookmarkAsync(context, reader.Id, work2Id);
        await EnsureBookmarkAsync(context, reader2.Id, work3Id);
    }

    private static async Task<int> EnsureWorkAsync(
        DbLibraryContext context,
        int userId,
        string title,
        string description,
        int ratingId,
        int statusId,
        IEnumerable<string> tagNames,
        IEnumerable<(int Number, string Title, string Content)> chapters)
    {
        var existing = await context.Fanfics.FirstOrDefaultAsync(f => f.UserId == userId && f.Title == title);
        Fanfic fanfic;

        if (existing == null)
        {
            fanfic = new Fanfic
            {
                UserId = userId,
                Title = title,
                Description = description,
                StatusId = statusId,
                ContentRatingId = ratingId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            context.Fanfics.Add(fanfic);
            await context.SaveChangesAsync();
        }
        else
        {
            fanfic = existing;
        }

        foreach (var tagName in tagNames.Distinct())
        {
            var tagId = await context.Tags.Where(t => t.Name == tagName).Select(t => t.Id).FirstAsync();
            var linkExists = await context.FanficTags.AnyAsync(ft => ft.FanficId == fanfic.Id && ft.TagId == tagId);
            if (!linkExists)
            {
                context.FanficTags.Add(new FanficTag
                {
                    FanficId = fanfic.Id,
                    TagId = tagId,
                    CreatedAt = DateTime.Now
                });
            }
        }

        foreach (var chapterInfo in chapters)
        {
            var chapterExists = await context.Chapters.AnyAsync(c =>
                c.FanficId == fanfic.Id && c.ChapterNumber == chapterInfo.Number);

            if (!chapterExists)
            {
                context.Chapters.Add(new Chapter
                {
                    FanficId = fanfic.Id,
                    ChapterNumber = chapterInfo.Number,
                    Title = chapterInfo.Title,
                    Content = chapterInfo.Content,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
        }

        await context.SaveChangesAsync();
        return fanfic.Id;
    }

    private static async Task EnsureFanficCommentAsync(DbLibraryContext context, int fanficId, int userId, string text)
    {
        var exists = await context.Comments.AnyAsync(c =>
            c.FanficId == fanficId && c.ChapterId == null && c.UserId == userId && c.Text == text && !c.IsDeleted);

        if (!exists)
        {
            context.Comments.Add(new Comment
            {
                FanficId = fanficId,
                ChapterId = null,
                UserId = userId,
                ParentCommentId = null,
                Text = text,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureChapterCommentAsync(DbLibraryContext context, int fanficId, int chapterId, int userId, string text)
    {
        var exists = await context.Comments.AnyAsync(c =>
            c.FanficId == fanficId && c.ChapterId == chapterId && c.UserId == userId && c.Text == text && !c.IsDeleted);

        if (!exists)
        {
            context.Comments.Add(new Comment
            {
                FanficId = fanficId,
                ChapterId = chapterId,
                UserId = userId,
                ParentCommentId = null,
                Text = text,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureLikeAsync(DbLibraryContext context, int userId, int fanficId)
    {
        if (!await context.Likes.AnyAsync(l => l.UserId == userId && l.FanficId == fanficId))
        {
            context.Likes.Add(new Like
            {
                UserId = userId,
                FanficId = fanficId,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureBookmarkAsync(DbLibraryContext context, int userId, int fanficId)
    {
        if (!await context.Bookmarks.AnyAsync(b => b.UserId == userId && b.FanficId == fanficId))
        {
            context.Bookmarks.Add(new Bookmark
            {
                UserId = userId,
                FanficId = fanficId,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureStatusesAsync(DbLibraryContext context)
    {
        if (!await context.FanficStatuses.AnyAsync(x => x.Name == "Draft"))
        {
            context.FanficStatuses.Add(new FanficStatus { Name = "Draft" });
        }

        if (!await context.FanficStatuses.AnyAsync(x => x.Name == "Published"))
        {
            context.FanficStatuses.Add(new FanficStatus { Name = "Published" });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureRatingsAsync(DbLibraryContext context)
    {
        if (!await context.ContentRatings.AnyAsync(x => x.Name == "General"))
        {
            context.ContentRatings.Add(new ContentRating { Name = "General" });
        }

        if (!await context.ContentRatings.AnyAsync(x => x.Name == "Teen"))
        {
            context.ContentRatings.Add(new ContentRating { Name = "Teen" });
        }

        if (!await context.ContentRatings.AnyAsync(x => x.Name == "Mature"))
        {
            context.ContentRatings.Add(new ContentRating { Name = "Mature" });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureTagsAsync(DbLibraryContext context)
    {
        foreach (var name in new[] { "romance", "friendship", "angst", "fantasy", "hurt/comfort", "humor", "drama", "adventure" })
        {
            if (!await context.Tags.AnyAsync(x => x.Name == name))
            {
                context.Tags.Add(new Tag { Name = name });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task<User> EnsureUserAsync(DbLibraryContext context, string username, string email, string password, string bio, string role)
    {
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
        {
            if (existing.Role != role)
            {
                existing.Role = role;
                await context.SaveChangesAsync();
            }

            return existing;
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = PasswordService.HashPassword(password),
            Role = role,
            Bio = bio,
            AvatarUrl = null,
            CreatedAt = DateTime.Now
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
