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
    [ApiController]
    // Base route can be just /api/tasks for individual task operations by ID
    // Operations related to a column will be under /api/columns/{columnId}/tasks
    public class TasksController : ControllerBase
    {
        private readonly TaskTrackerDbContext _context;
        private readonly UserManager<User> _userManager;

        public TasksController(TaskTrackerDbContext context, UserManager<User> userManager)
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

        // Helper to check if the user has access to the column (via the project)
        private async Task<bool> UserHasAccessToColumn(int columnId, int userId)
        {
            var column = await _context.Columns
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.ColumnId == columnId);
            
            return column != null && column.Project != null && column.Project.OwnerUserId == userId;
        }

        // Helper to check if the user has access to the task (via the column and project)
        private async Task<(bool hasAccess, Models.Task? task)> UserHasAccessToTask(int taskId, int userId)
        {
            var task = await _context.Tasks
                .Include(t => t.Column)
                    .ThenInclude(c => c!.Project) 
                .FirstOrDefaultAsync(t => t.TaskId == taskId);

            if (task == null || task.Column == null || task.Column.Project == null)
            {
                return (false, null);
            }
            return (task.Column.Project.OwnerUserId == userId, task);
        }


        // GET: api/columns/{columnId}/tasks
        [HttpGet("api/columns/{columnId}/tasks")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksForColumn(int columnId)
        {
            var userId = GetUserId();
            if (!await UserHasAccessToColumn(columnId, userId))
            {
                return Forbid("You do not have access to this column or project.");
            }

            var tasks = await _context.Tasks
                .Where(t => t.ColumnId == columnId)
                .Select(t => new TaskDto
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    Description = t.Description,
                    Deadline = t.Deadline,
                    ColumnId = t.ColumnId,
                    Order = t.Order
                })
                .OrderBy(t => t.Order)
                .ToListAsync();

            return Ok(tasks);
        }

        // GET: api/tasks/{id}
        [HttpGet("api/tasks/{id}")]
        public async Task<ActionResult<TaskDto>> GetTask(int id)
        {
            var userId = GetUserId();
            var (hasAccess, task) = await UserHasAccessToTask(id, userId);

            if (!hasAccess)
            {
                return Forbid("You do not have access to this task or it does not exist.");
            }
            if (task == null) // Should be covered by !hasAccess if task not found
            {
                return NotFound(new { Message = $"Task with ID {id} not found." });
            }

            return Ok(new TaskDto
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                Deadline = task.Deadline,
                ColumnId = task.ColumnId,
                Order = task.Order
            });
        }

        // POST: api/columns/{columnId}/tasks
        [HttpPost("api/columns/{columnId}/tasks")]
        public async Task<ActionResult<TaskDto>> CreateTask(int columnId, TaskCreateDto taskCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (!await UserHasAccessToColumn(columnId, userId))
            {
                return Forbid("You do not have access to this column or project, or the column does not exist.");
            }
            
            var column = await _context.Columns.Include(c => c.Tasks).FirstOrDefaultAsync(c => c.ColumnId == columnId);
            if (column == null) return NotFound(new { Message = $"Column with ID {columnId} not found."});


            var task = new Models.Task // Fully qualify to avoid conflict with System.Threading.Tasks.Task
            {
                Title = taskCreateDto.Title,
                Description = taskCreateDto.Description,
                Deadline = taskCreateDto.Deadline,
                ColumnId = columnId,
                Order = column.Tasks.Any() ? column.Tasks.Max(t => t.Order) + 1 : 0 // Assign order
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            var taskDto = new TaskDto
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                Deadline = task.Deadline,
                ColumnId = task.ColumnId,
                Order = task.Order
            };
            // Route to GetTask: needs "id" parameter
            return CreatedAtAction(nameof(GetTask), new { id = task.TaskId }, taskDto);
        }

        // PUT: api/tasks/{id}
        [HttpPut("api/tasks/{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskUpdateDto taskUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            if (id != taskUpdateDto.TaskId) // Assuming TaskUpdateDto includes TaskId for safety, though not in definition
            {
                // If TaskUpdateDto had TaskId: return BadRequest("Task ID mismatch.");
            }

            var userId = GetUserId();
            var (hasAccess, task) = await UserHasAccessToTask(id, userId);

            if (!hasAccess)
            {
                return Forbid("You do not have access to this task or it does not exist.");
            }
            if (task == null)
            {
                return NotFound(new { Message = $"Task with ID {id} not found." });
            }

            // Check if column is being changed and if user has access to the new column
            if (task.ColumnId != taskUpdateDto.ColumnId)
            {
                if (!await UserHasAccessToColumn(taskUpdateDto.ColumnId, userId))
                {
                    return Forbid("You do not have access to the target column.");
                }
                 // Adjust order in old and new column if implementing move logic here
                 // For simplicity, this PUT is for updates, MoveTask handles moves.
                 // If allowing column change here, then reordering logic is needed.
                 // For now, let's assume this PUT doesn't change ColumnId directly, or if it does, order needs careful handling.
                 // The provided TaskUpdateDto includes ColumnId, so we need to handle it.
                 // This simplified version will update ColumnId but not reorder in old/new columns.
                 // A dedicated move endpoint is better for that.
            }


            task.Title = taskUpdateDto.Title;
            task.Description = taskUpdateDto.Description;
            task.Deadline = taskUpdateDto.Deadline;
            // task.ColumnId = taskUpdateDto.ColumnId; // If allowing direct column change
            // task.Order = taskUpdateDto.Order; // If allowing direct order change

            _context.Entry(task).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var (existsAccess, _) = await UserHasAccessToTask(id, userId);
                if (!existsAccess) // Check if it still exists and user still has access
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

        // PUT: api/tasks/{id}/move
        [HttpPut("api/tasks/{id}/move")]
        public async Task<IActionResult> MoveTask(int id, TaskMoveDto taskMoveDto)
        {
            var userId = GetUserId();
            var (hasAccessToTask, task) = await UserHasAccessToTask(id, userId);

            if (!hasAccessToTask || task == null)
            {
                return Forbid("You do not have access to this task, or it does not exist.");
            }

            if (!await UserHasAccessToColumn(taskMoveDto.TargetColumnId, userId))
            {
                return Forbid("You do not have access to the target column.");
            }
            
            var sourceColumnId = task.ColumnId;
            var targetColumnId = taskMoveDto.TargetColumnId;

            // Remove from old column's order and update orders
            var tasksInSourceColumn = await _context.Tasks
                .Where(t => t.ColumnId == sourceColumnId && t.TaskId != id)
                .OrderBy(t => t.Order)
                .ToListAsync();

            int i = 0;
            foreach (var t in tasksInSourceColumn)
            {
                if (t.Order >= task.Order) // Only re-index those after the removed task
                {
                     // No, re-index all based on their current relative order
                }
                 t.Order = i++; // Re-index all remaining tasks in source column
                _context.Entry(t).State = EntityState.Modified;
            }

            // Add to new column's order and update orders
            var tasksInTargetColumn = await _context.Tasks
                .Where(t => t.ColumnId == targetColumnId)
                .OrderBy(t => t.Order)
                .ToListAsync();

            // Make space for the new task at its desired order
            foreach (var t in tasksInTargetColumn)
            {
                if (t.Order >= taskMoveDto.NewOrderInColumn)
                {
                    t.Order++;
                    _context.Entry(t).State = EntityState.Modified;
                }
            }
            
            task.ColumnId = targetColumnId;
            task.Order = taskMoveDto.NewOrderInColumn;
            _context.Entry(task).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException ex)
            {
                 // Log the exception
                return StatusCode(StatusCodes.Status500InternalServerError, new {Message = "Error updating database.", Details = ex.Message});
            }

            return NoContent();
        }


        // PUT: api/columns/{columnId}/tasks/reorder
        [HttpPut("api/columns/{columnId}/tasks/reorder")]
        public async Task<IActionResult> ReorderTasks(int columnId, TaskOrderUpdateDto taskOrderUpdateDto)
        {
            if (taskOrderUpdateDto.TaskIdsInOrder == null || !taskOrderUpdateDto.TaskIdsInOrder.Any())
            {
                return BadRequest(new { Message = "Task IDs list cannot be empty." });
            }

            var userId = GetUserId();
            if (!await UserHasAccessToColumn(columnId, userId))
            {
                return Forbid("You do not have access to this column or project.");
            }

            var tasksInColumn = await _context.Tasks
                                    .Where(t => t.ColumnId == columnId)
                                    .ToListAsync();
            
            if(taskOrderUpdateDto.TaskIdsInOrder.Distinct().Count() != taskOrderUpdateDto.TaskIdsInOrder.Count)
            {
                return BadRequest(new { Message = "Duplicate task IDs found in the reorder list." });
            }

            if(taskOrderUpdateDto.TaskIdsInOrder.Count != tasksInColumn.Count || 
               !taskOrderUpdateDto.TaskIdsInOrder.All(id => tasksInColumn.Any(t => t.TaskId == id)))
            {
                return BadRequest(new { Message = "The provided task IDs do not match the tasks in the column." });
            }


            for (int i = 0; i < taskOrderUpdateDto.TaskIdsInOrder.Count; i++)
            {
                var taskId = taskOrderUpdateDto.TaskIdsInOrder[i];
                var task = tasksInColumn.FirstOrDefault(t => t.TaskId == taskId);
                if (task != null)
                {
                    task.Order = i;
                    _context.Entry(task).State = EntityState.Modified;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { Message = "Data was modified by another user. Please refresh and try again."});
            }
            return NoContent();
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("api/tasks/{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetUserId();
            var (hasAccess, task) = await UserHasAccessToTask(id, userId);

            if (!hasAccess)
            {
                return Forbid("You do not have access to this task or it does not exist.");
            }
             if (task == null) // Should be covered by !hasAccess if task not found
            {
                return NotFound(new { Message = $"Task with ID {id} not found." });
            }


            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            // Optionally, re-index tasks in the column
            var remainingTasks = await _context.Tasks
                                    .Where(t => t.ColumnId == task.ColumnId)
                                    .OrderBy(t => t.Order)
                                    .ToListAsync();
            for(int i=0; i < remainingTasks.Count; i++)
            {
                remainingTasks[i].Order = i;
                _context.Entry(remainingTasks[i]).State = EntityState.Modified;
            }
            await _context.SaveChangesAsync();


            return NoContent();
        }
    }
}
