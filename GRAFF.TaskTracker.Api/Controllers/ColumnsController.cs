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
    [Route("api/projects/{projectId}/columns")]
    [ApiController]
    public class ColumnsController : ControllerBase
    {
        private readonly TaskTrackerDbContext _context;
        private readonly UserManager<User> _userManager;

        public ColumnsController(TaskTrackerDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private int GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                throw new ArgumentException("User ID not found or invalid in token.");
            }
            return userId;
        }

        private async Task<bool> ProjectBelongsToUser(int projectId, int userId)
        {
            return await _context.Projects.AnyAsync(p => p.ProjectId == projectId && p.OwnerUserId == userId);
        }

        // GET: api/projects/{projectId}/columns
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ColumnDto>>> GetColumns(int projectId)
        {
            var userId = GetUserId();
            if (!await ProjectBelongsToUser(projectId, userId))
            {
                return Forbid("You do not have access to this project.");
            }

            var columns = await _context.Columns
                .Where(c => c.ProjectId == projectId)
                .Include(c => c.Tasks)
                .Select(c => new ColumnDto
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
                })
                .OrderBy(c => c.Order)
                .ToListAsync();

            return Ok(columns);
        }

        // GET: api/projects/{projectId}/columns/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ColumnDto>> GetColumn(int projectId, int id)
        {
            var userId = GetUserId();
            if (!await ProjectBelongsToUser(projectId, userId))
            {
                return Forbid("You do not have access to this project.");
            }

            var column = await _context.Columns
                .Include(c => c.Tasks)
                .Where(c => c.ProjectId == projectId && c.ColumnId == id)
                .Select(c => new ColumnDto
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
                })
                .FirstOrDefaultAsync();

            if (column == null)
            {
                return NotFound(new { Message = $"Column with ID {id} in Project {projectId} not found." });
            }
            return Ok(column);
        }

        // POST: api/projects/{projectId}/columns
        [HttpPost]
        public async Task<ActionResult<ColumnDto>> CreateColumn(int projectId, ColumnCreateDto columnCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (!await ProjectBelongsToUser(projectId, userId))
            {
                return Forbid("You do not have access to this project or it does not exist.");
            }

            var project = await _context.Projects.Include(p => p.Columns).FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (project == null) return NotFound(new { Message = $"Project with ID {projectId} not found."});


            var column = new Column
            {
                Name = columnCreateDto.Name,
                ProjectId = projectId,
                Order = project.Columns.Any() ? project.Columns.Max(c => c.Order) + 1 : 0 // Assign order
            };

            _context.Columns.Add(column);
            await _context.SaveChangesAsync();

            var columnDto = new ColumnDto
            {
                ColumnId = column.ColumnId,
                Name = column.Name,
                Order = column.Order,
                ProjectId = column.ProjectId,
                Tasks = new List<TaskDto>() // Initially no tasks
            };

            return CreatedAtAction(nameof(GetColumn), new { projectId = projectId, id = column.ColumnId }, columnDto);
        }

        // PUT: api/projects/{projectId}/columns/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateColumn(int projectId, int id, ColumnUpdateDto columnUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (!await ProjectBelongsToUser(projectId, userId))
            {
                return Forbid("You do not have access to this project.");
            }

            var column = await _context.Columns.FirstOrDefaultAsync(c => c.ColumnId == id && c.ProjectId == projectId);
            if (column == null)
            {
                return NotFound(new { Message = $"Column with ID {id} in Project {projectId} not found." });
            }

            column.Name = columnUpdateDto.Name;
            // Order update is handled by ReorderColumns, but you could allow individual order update here if needed.
            // column.Order = columnUpdateDto.Order; 

            _context.Entry(column).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Columns.AnyAsync(e => e.ColumnId == id && e.ProjectId == projectId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        // PUT: api/projects/{projectId}/columns/reorder
        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderColumns(int projectId, ColumnOrderUpdateDto columnOrderUpdateDto)
        {
            if (columnOrderUpdateDto.ColumnIdsInOrder == null || !columnOrderUpdateDto.ColumnIdsInOrder.Any())
            {
                return BadRequest(new {Message = "Column IDs list cannot be empty."});
            }
            
            var userId = GetUserId();
            if (!await ProjectBelongsToUser(projectId, userId))
            {
                return Forbid("You do not have access to this project.");
            }

            var columnsInProject = await _context.Columns
                                        .Where(c => c.ProjectId == projectId)
                                        .ToListAsync();

            if(columnOrderUpdateDto.ColumnIdsInOrder.Distinct().Count() != columnOrderUpdateDto.ColumnIdsInOrder.Count)
            {
                return BadRequest(new { Message = "Duplicate column IDs found in the reorder list." });
            }

            if(columnOrderUpdateDto.ColumnIdsInOrder.Count != columnsInProject.Count || 
               !columnOrderUpdateDto.ColumnIdsInOrder.All(id => columnsInProject.Any(c => c.ColumnId == id)))
            {
                return BadRequest(new { Message = "The provided column IDs do not match the columns in the project." });
            }

            for (int i = 0; i < columnOrderUpdateDto.ColumnIdsInOrder.Count; i++)
            {
                var columnId = columnOrderUpdateDto.ColumnIdsInOrder[i];
                var column = columnsInProject.FirstOrDefault(c => c.ColumnId == columnId);
                if (column != null)
                {
                    column.Order = i;
                    _context.Entry(column).State = EntityState.Modified;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle concurrency issues, e.g., if a column was deleted or project changed.
                return Conflict(new {Message = "Data was modified by another user. Please refresh and try again."});
            }
            return NoContent();
        }


        // DELETE: api/projects/{projectId}/columns/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteColumn(int projectId, int id)
        {
            var userId = GetUserId();
            if (!await ProjectBelongsToUser(projectId, userId))
            {
                return Forbid("You do not have access to this project.");
            }

            var column = await _context.Columns
                .Include(c => c.Tasks) // Ensure tasks are loaded for cascade delete
                .FirstOrDefaultAsync(c => c.ColumnId == id && c.ProjectId == projectId);

            if (column == null)
            {
                return NotFound(new { Message = $"Column with ID {id} in Project {projectId} not found." });
            }

            _context.Columns.Remove(column);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
