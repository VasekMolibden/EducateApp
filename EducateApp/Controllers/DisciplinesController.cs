using ClosedXML.Excel;
using EducateApp.Models;
using EducateApp.Models.Data;
using EducateApp.ViewModels.Disciplines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EducateApp.Controllers
{
    [Authorize(Roles = "admin, registeredUser")]
    public class DisciplinesController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly UserManager<User> _userManager;

        public DisciplinesController(ApplicationContext context, UserManager<User> user)
        {
            _context = context;
            _userManager = user;
        }

        // GET: Disciplines
        public async Task<IActionResult> Index()
        {
            // находим информацию о пользователе, который вошел в систему по его имени
            IdentityUser user = await _userManager.FindByNameAsync(HttpContext.User.Identity.Name);

            // через контекст данных получаем доступ к таблице
            var appCtx = _context.Disciplines
                .Include(d => d.User)
                .Where(w => w.IdUser == user.Id)
                .OrderBy(o => o.ProfModule);

            // возвращаем в представление полученный список записей
            return View(await appCtx.ToListAsync());
        }

        // GET: Disciplines/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Disciplines/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDisciplineViewModel model)
        {
            IdentityUser user = await _userManager.FindByNameAsync(HttpContext.User.Identity.Name);

            if (_context.Disciplines
                .Where(f => f.IdUser == user.Id &&
                 f.IndexProfModule == model.IndexProfModule &&
                 f.ProfModule == model.ProfModule &&
                 f.Index == model.Index && f.Name == model.Name &&
                 f.ShortName == model.ShortName).FirstOrDefault() != null)
            {
                ModelState.AddModelError("", "Введенный вид дисциплины уже существует");
            }

            if (ModelState.IsValid)
            {
                Discipline discipline = new()
                {
                    IndexProfModule = model.IndexProfModule,
                    ProfModule = model.ProfModule,
                    Index = model.Index,
                    Name = model.Name,
                    ShortName = model.ShortName,
                    IdUser = user.Id
                };

                _context.Add(discipline);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Disciplines/Edit/5
        public async Task<IActionResult> Edit(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var disciplines = await _context.Disciplines.FindAsync(id);
            if (disciplines == null)
            {
                return NotFound();
            }

            EditDisciplineViewModel model = new()
            {
                Id = disciplines.Id,
                IndexProfModule = disciplines.IndexProfModule,
                ProfModule = disciplines.ProfModule,
                Index = disciplines.Index,
                Name = disciplines.Name,
                ShortName = disciplines.ShortName,
                IdUser = disciplines.IdUser
            };

            return View(model);
        }

        // POST: Disciplines/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(short id, EditDisciplineViewModel model)
        {
            Discipline discipline = await _context.Disciplines.FindAsync(id);

            if (id != discipline.Id)
            {
                return NotFound();
            }

            IdentityUser user = await _userManager.FindByNameAsync(HttpContext.User.Identity.Name);

            if (_context.Disciplines
                .Where(f => f.IdUser == user.Id &&
                 f.IndexProfModule == model.IndexProfModule &&
                 f.ProfModule == model.ProfModule &&
                 f.Index == model.Index && f.Name == model.Name &&
                 f.ShortName == model.ShortName).FirstOrDefault() != null)
            {
                ModelState.AddModelError("", "Введенный вид дисциплины уже существует");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    discipline.IndexProfModule = model.IndexProfModule;
                    discipline.ProfModule = model.ProfModule;
                    discipline.Index = model.Index;
                    discipline.Name = model.Name;
                    discipline.ShortName = model.ShortName;
                    _context.Update(discipline);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DisciplineExists(discipline.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Disciplines/Delete/5
        public async Task<IActionResult> Delete(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discipline = await _context.Disciplines
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (discipline == null)
            {
                return NotFound();
            }

            return View(discipline);
        }

        // POST: Disciplines/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            var discipline = await _context.Disciplines.FindAsync(id);
            _context.Disciplines.Remove(discipline);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Disciplines/Details/5
        public async Task<IActionResult> Details(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discipline = await _context.Disciplines
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (discipline == null)
            {
                return NotFound();
            }

            return PartialView(discipline);
        }

        public async Task<FileResult> DownloadPattern()
        {
            IdentityUser user = await _userManager.FindByNameAsync(HttpContext.User.Identity.Name);

            // выбираем из базы данных все специальности текущего пользователя
            var appCtx = _context.Disciplines
                .Include(s => s.User)
                .Where(w => w.IdUser == user.Id)
                .OrderBy(f => f.Name)    // сортируем записи
                .ThenBy(f => f.IndexProfModule);

            int i = 1;      // счетчик

            IXLRange rngBorder;     // объект для работы с диапазонами в Excel (выделение групп ячеек)

            // создание книги Excel
            using (XLWorkbook workbook = new(XLEventTracking.Disabled))
            {
                // для каждой специальности 
                foreach (Discipline discipline in appCtx)
                {
                    // добавить лист в книгу Excel
                    // с названием 3 символа формы обучения и кода специальности
                    IXLWorksheet worksheet = workbook.Worksheets
                        .Add($"Дисциплины");

                    // заголовки у столбцов
                    worksheet.Cell("A" + i).Value = "Индекс профессионального модуля";
                    worksheet.Cell("B" + i).Value = "Профессиональный модуль";
                    worksheet.Cell("C" + i).Value = "Индекс";
                    worksheet.Cell("D" + i).Value = "Название";
                    worksheet.Cell("E" + i).Value = "Краткое название";

                    //i++;

                    // устанавливаем внешние границы для диапазона A4:F4
                    rngBorder = worksheet.Range("A1:E1");       // создание диапазона (выделения ячеек)
                    rngBorder.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;       // для диапазона задаем внешнюю границу

                    // на листе для столбцов задаем значение ширины по содержимому
                    worksheet.Columns().AdjustToContents();

                    // счетчик "обнуляем"
                    i = 1;
                }

                // создаем стрим
                using (MemoryStream stream = new())
                {
                    // помещаем в стрим созданную книгу
                    workbook.SaveAs(stream);
                    stream.Flush();

                    // возвращаем файл определенного типа
                    return new FileContentResult(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                    {
                        FileDownloadName = $"disciplines_{DateTime.UtcNow.ToShortDateString()}.xlsx"     //в названии файла указываем таблицу и текущую дату
                    };
                }
            }
        }

        private bool DisciplineExists(short id)
        {
            return _context.Disciplines.Any(e => e.Id == id);
        }
    }
}
