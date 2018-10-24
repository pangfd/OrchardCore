using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Autoroute.Model;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.Lists.Models;
using OrchardCore.Tests.Apis.Context;
using Xunit;

namespace OrchardCore.Tests.Apis.GraphQL.Blog
{
    public class BlogPostTests
    {
        [Fact]
        public async Task ShouldListAllBlogs()
        {
            using (var context = new BlogContext())
            {
                await context.InitializeAsync();

                var result = await context
                    .GraphQLClient
                    .Content
                    .Query("Blog", builder =>
                    {
                        builder
                            .AddField("contentItemId");
                    });

                Assert.Single(result["data"]["blog"].Children()["contentItemId"]
                    .Where(b => b.ToString() == context.BlogContentItemId));
            }
        }

        [Fact]
        public async Task ShouldQueryByBlogPostAutoroutePart()
        {
            using (var context = new BlogContext())
            {
                await context.InitializeAsync();

                var blogPostContentItemId1 = await context
                    .CreateContentItem("BlogPost", builder =>
                    {
                        builder
                            .DisplayText = "Some sorta blogpost!";

                        builder
                            .Weld(new AutoroutePart
                            {
                                Path = "Path1"
                            });

                        builder
                            .Weld(new ContainedPart
                            {
                                ListContentItemId = context.BlogContentItemId
                            });
                    });

                var blogPostContentItemId2 = await context
                    .CreateContentItem("BlogPost", builder =>
                    {
                        builder
                            .DisplayText = "Some sorta other blogpost!";

                        builder
                            .Weld(new AutoroutePart
                            {
                                Path = "Path2"
                            });

                        builder
                            .Weld(new ContainedPart
                            {
                                ListContentItemId = context.BlogContentItemId
                            });
                    });

                var result = await context
                    .GraphQLClient
                    .Content
                    .Query("BlogPost", builder =>
                    {
                        builder
                            .WithNestedQueryField("AutoroutePart", "path: \"Path1\"");

                        builder
                            .AddField("DisplayText");
                    });

                Assert.Equal(
                    "Some sorta blogpost!",
                    result["data"]["blogPost"][0]["displayText"].ToString());
            }
        }

        [Fact]
        public async Task WhenThePartHasTheSameNameAsTheContentTypeShouldCollapseFieldsToContentType()
        {
            using (var context = new BlogContext())
            {
                await context.InitializeAsync();

                var result = await context
                    .GraphQLClient
                    .Content
                    .Query("BlogPost", builder =>
                    {
                        builder.AddField("Subtitle");
                    });

                Assert.Equal(
                    "Problems look mighty small from 150 miles up",
                    result["data"]["blogPost"][0]["subtitle"].ToString());
            }
        }

        [Fact]
        public async Task WhenCreatingABlogPostShouldBeAbleToPopulateField()
        {
            using (var context = new BlogContext())
            {
                await context.InitializeAsync();

                var blogPostContentItemId = await context
                    .CreateContentItem("BlogPost", builder =>
                    {
                        builder
                            .DisplayText = "Some sorta blogpost!";

                        builder
                            .Weld("BlogPost", new ContentPart());

                        builder
                            .Alter<ContentPart>("BlogPost", (cp) =>
                            {
                                cp.Weld("Subtitle", new TextField());

                                cp.Alter<TextField>("Subtitle", tf =>
                                {
                                    tf.Text = "Hey - Is this working!?!?!?!?";
                                });
                            });

                        builder
                            .Weld(new ContainedPart
                            {
                                ListContentItemId = context.BlogContentItemId
                            });
                    });

                var result = await context
                    .GraphQLClient
                    .Content
                    .Query("BlogPost", builder =>
                    {
                        builder
                            .WithQueryField("ContentItemId", blogPostContentItemId);

                        builder
                            .AddField("Subtitle");
                    });

                Assert.Equal(
                    "Hey - Is this working!?!?!?!?",
                    result["data"]["blogPost"][0]["subtitle"].ToString());
            }
        }
    }
}
