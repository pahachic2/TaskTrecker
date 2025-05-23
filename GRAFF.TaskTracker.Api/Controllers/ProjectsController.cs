using GRAFF.TaskTracker.Api.Data;
using GRAFF.TaskTracker.Api.DTOs;
using GRAFF.TaskTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GRAFF.TaskTracker.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly TaskTrackerDbContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectsController(TaskTrackerDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private int GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                // This should ideally not happen if [Authorize] is working correctly and token is valid.
                throw new ArgumentException("User ID not found or invalid in token.");
            }
            return userId;
        }

        // GET: api/Projects
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            var userId = GetUserId();
            var projects = await _context.Projects
                                .Where(p => p.OwnerUserId == userId)
                                .Include(p => p.Columns)
                                    .ThenInclude(c => c.Tasks)
                                .Select(p => new ProjectDto
                                {
                                    ProjectId = p.ProjectId,
                                    Name = p.Name,
                                    OwnerUserId = p.OwnerUserId,
                                    Columns = p.Columns.Select(c => new ColumnDto
                                    {
                                        ColumnId = c.ColumnId,
                                        Name = c.Name,
                                        Order = c.Order,
                                        ProjectId = c.ProjectId,
                                        Tasks = c.Tasks.Select(t => new TaskDto
                                        {
                                            TaskId = t.TaskId,
                                            Title = t.Title,
                                            Description = t.Description,
                                            Deadline = t.Deadline,
                                            ColumnId = t.ColumnId,
                                            Order = t.Order
                                        }).OrderBy(t => t.Order).ToList()
                                    }).OrderBy(c => c.Order).ToList()
                                })
                                .ToListAsync();
            return Ok(projects);
        }

        // GET: api/Projects/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            var userId = GetUserId();
            var project = await _context.Projects
                .Include(p => p.Columns)
                    .ThenInclude(c => c.Tasks)
                .Where(p => p.ProjectId == id && p.OwnerUserId == userId)
                .Select(p => new ProjectDto
                {
                    ProjectId = p.ProjectId,
                    Name = p.Name,
                    OwnerUserId = p.OwnerUserId,
                    Columns = p.Columns.Select(c => new ColumnDto
                    {
                        ColumnId = c.ColumnId,
                        Name = c.Name,
                        Order = c.Order,
                        ProjectId = c.ProjectId,
                        Tasks = c.Tasks.Select(t => new TaskDto
                        {
                            TaskId = t.TaskId,
                            Title = t.Title,
                            Description = t.Description,
                            Deadline = t.Deadline,
                            ColumnId = t.ColumnId,
                            Order = t.Order
                        }).OrderBy(t => t.Order).ToList()
                    }).OrderBy(c => c.Order).ToList()
                })
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return NotFound(new { Message = $"Project with ID {id} not found or you do not have permission to access it." });
            }
            return Ok(project);
        }

        // POST: api/Projects
        [HttpPost]
        public async Task<ActionResult<ProjectDto>> CreateProject(ProjectCreateDto projectCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var project = new Project
            {
                Name = projectCreateDto.Name,
                OwnerUserId = userId
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Map to ProjectDto to return the created resource representation
            var projectDto = new ProjectDto
            {
                ProjectId = project.ProjectId,
                Name = project.Name,
                OwnerUserId = project.OwnerUserId,
                Columns = new List<ColumnDto>() // Initially no columns
            };

            return CreatedAtAction(nameof(GetProject), new { id = project.ProjectId }, projectDto);
        }

        // PUT: api/Projects/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, ProjectUpdateDto projectUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == id && p.OwnerUserId == userId);

            if (project == null)
            {
                return NotFound(new { Message = $"Project with ID {id} not found or you do not have permission to modify it." });
            }

            project.Name = projectUpdateDto.Name;
            _context.Entry(project).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProjectExists(id, userId))
                {
                    return NotFound();
                }
                else
                {
                    throw; // Or handle more gracefully
                }
            }
            return NoContent();
        }

        // DELETE: api/Projects/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = GetUserId();
            var project = await _context.Projects
                .Include(p => p.Columns) // Include columns to ensure they are loaded for cascade delete if not handled by DB
                    .ThenInclude(c => c.Tasks) // Include tasks for the same reason
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.OwnerUserId == userId);

            if (project == null)
            {
                return NotFound(new { Message = $"Project with ID {id} not found or you do not have permission to delete it." });
            }

            // EF Core should handle cascade deletes based on the model configuration (OnDelete(DeleteBehavior.Cascade))
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> ProjectExists(int id, int userId)
        {
            return await _context.Projects.AnyAsync(e => e.ProjectId == id && e.OwnerUserId == userId);
        }
    }
}
