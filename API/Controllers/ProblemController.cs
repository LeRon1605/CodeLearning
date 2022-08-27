﻿using API.Extension;
using API.Filter;
using API.Helper;
using API.Migrations;
using API.Models.DTO;
using API.Services;
using AutoMapper;
using CodeStudy.Models;
using Data.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace API.Controllers
{
    [Route("problems")]
    [ApiController]
    public class ProblemController : ControllerBase
    {
        private readonly IProblemService _problemService;
        private readonly ITagService _tagSerivce;
        private readonly ITestcaseService _testcaseService;
        private readonly ISubmissionService _submissionService;
        private readonly IReportService _reportService;
        private readonly IMapper _mapper;
        private readonly ILogger<ProblemController> _logger;

        public ProblemController(IProblemService problemService, ITagService tagService, ISubmissionService submissionService, ITestcaseService testcaseService, IReportService reportService, IMapper mapper, ILogger<ProblemController> logger)
        {
            _problemService = problemService;
            _tagSerivce = tagService;
            _submissionService = submissionService;
            _testcaseService = testcaseService;
            _reportService = reportService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromServices] IDistributedCache cache, string name = "", string tag = "", int? page = null, int pageSize = 5)
        {
            if (page != null)
            {
                PagingList<Problem> list = await _problemService.GetPage((int)page, pageSize, tag, name);
                return Ok(_mapper.Map<PagingList<Problem>, PagingList<ProblemDTO>>(list));
            }
            else
            {
                CacheData data = await cache.GetRecordAsync<CacheData>("problems");
                if (data == null)
                {
                    data = new CacheData
                    {
                        RecordID = "problems",
                        Data = _mapper.Map<IEnumerable<Problem>, IEnumerable<ProblemDTO>>(_problemService.GetAll()),
                        CacheAt = DateTime.Now,
                        ExpireAt = DateTime.Now.AddSeconds(30),
                    };
                    await cache.SetRecordAsync("problems", data, TimeSpan.FromSeconds(30));
                }
                return Ok(data);
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(ProblemInput input)
        {
            string[] tags = input.Tags.Where(x => _tagSerivce.FindByID(x) == null).ToArray();
            if (tags.Count() > 0)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Create problem failed.",
                    detail = new
                    {
                        message = "Tags does not exist.",
                        value = tags
                    }
                });
            }
            Problem problem = new Problem
            {
                ID = Guid.NewGuid().ToString(),
                Name = input.Name,
                Status = false,
                Description = input.Description,
                ArticleID = User.FindFirst(Constant.ID).Value,
                CreatedAt = DateTime.Now,
                TestCases = input.TestCases.Select(x => new TestCase
                {
                    ID = Guid.NewGuid().ToString(),
                    Input = x.Input,
                    Output = x.Output,
                    CreatedAt = DateTime.Now,
                    MemoryLimit = x.MemoryLimit,
                    TimeLimit = x.TimeLimit,
                }).ToList(),
                Tags = input.Tags.Select(x => _tagSerivce.FindByID(x)).ToList()
            };
            await _problemService.Add(problem);
            return Ok();
        }

        [HttpGet("{ID}")]
        public IActionResult GetByID(string ID)
        {
            Problem problem = _problemService.FindByID(ID);
            if (problem == null)
            {
                return NotFound(new ErrorResponse { 
                    error = "Resource not found.",
                    detail = "Problem does not exist."
                });
            }
            problem.Tags = _problemService.GetTagsOfProblem(ID);
            return Ok(_mapper.Map<Problem, ProblemDTO>(problem));
        }

        [HttpDelete("{ID}")]
        [Authorize]
        public async Task<IActionResult> Delete(string ID)
        {
            Problem problem = _problemService.FindByID(ID);
            if (problem == null)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Resource not found.",
                    detail = "Problem does not exist."
                });
            }
            if (problem.ArticleID == User.FindFirst(Constant.ID)?.Value || User.FindFirst(Constant.ROLE)?.Value == Constant.ADMIN)
            {
                await _problemService.Remove(ID);
                return NoContent();
            }
            else
            {
                return Forbid();
            }    
        }

        [HttpPut("{ID}")]
        [Authorize]
        public async Task<IActionResult> Update(string ID, ProblemInputUpdate input)
        {
            Problem problem = _problemService.FindByID(ID);
            if (problem == null)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Resource not found.",
                    detail = "Problem does not exist."
                });
            }
            string[] tags = input.Tags.Where(x => _tagSerivce.FindByID(x) == null).ToArray();
            if (tags.Count() > 0)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Create problem failed.",
                    detail = new
                    {
                        message = "Tags does not exist.",
                        value = tags
                    }
                });
            }
            if (problem.ArticleID == User.FindFirst(Constant.ID)?.Value || User.FindFirst(Constant.ROLE)?.Value == Constant.ADMIN)
            {
                await _problemService.Update(ID, input);
                return NoContent();
            }
            else
            {
                return Forbid();
            }
        }

        [HttpGet("{ID}/testcases")]
        [Authorize]
        public IActionResult GetTestCases(string ID)
        {
            Problem problem = _problemService.FindByID(ID);
            if (problem == null)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Resource not found.",
                    detail = "Problem does not exist."
                });
            }
            if (problem.ArticleID == User.FindFirst(Constant.ID)?.Value || User.FindFirst(Constant.ROLE)?.Value == Constant.ADMIN)
            {

                return Ok(_mapper.Map<IEnumerable<TestCase>, IEnumerable<TestcaseDTO>>(_testcaseService.GetTestcaseOfProblem(ID)));
            }
            else
            {
                return Forbid();
            }
        }

        [HttpPost("{ID}/testcases")]
        [Authorize]
        public async Task<IActionResult> CreateTestCase(string ID, TestcaseInput input)
        {
            Problem problem = _problemService.FindByID(ID);
            if (problem == null)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Resource not found.",
                    detail = "Problem does not exist."
                });
            }
            if (problem.ArticleID == User.FindFirst(Constant.ID).Value || User.FindFirst(Constant.ROLE).Value == Constant.ADMIN)
            {
                await _testcaseService.Add(new TestCase
                {
                    ID = Guid.NewGuid().ToString(),
                    Input = input.Input,
                    Output = input.Output,
                    CreatedAt = DateTime.Now,
                    MemoryLimit = input.MemoryLimit,
                    TimeLimit = input.TimeLimit,
                    ProblemID = ID
                });
                return Ok();
            }
            else
            {
                return Forbid();
            }
        }

        [HttpPost("{ID}/submissions")]
        [Authorize]
        public async Task<IActionResult> Submit(string ID, SubmissionInput input)
        {
            Problem problem = _problemService.FindByID(ID);
            if (problem == null)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Resource not found.",
                    detail = "Problem does not exist."
                });
            }
            Submission submission = await _submissionService.Submit(new Submission
            {
                ID = Guid.NewGuid().ToString(),
                Status = false,
                UserID = User.FindFirst(Constant.ID).Value,
                Code = input.Code,
                Language = input.Language,
                CreatedAt = DateTime.Now,
                SubmissionDetails = new List<SubmissionDetail>()
            }, ID);
            
            return Ok(_mapper.Map<Submission, SubmissionDTO>(submission));
        }

        [HttpPost("{ID}/submissions")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> Submit(string ID, IFormFile file)
        {
            Problem problem = _problemService.FindByID(ID);
            if (problem == null)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Resource not found.",
                    detail = "Problem does not exist."
                });
            }
            if (file != null && file.Length > 0)
            {
                string code = null;
                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    code = await stream.ReadToEndAsync();
                };
                if (code == null)
                {
                    _logger.LogWarning("Cant read content from {File} At {Date} while Content Length is {Length}", file.FileName, DateTime.UtcNow, file.Length);
                    return BadRequest(new
                    {
                        error = "Submit failed.",
                        detail = "Invalid file, can't read content from file."
                    });
                }
                Submission submission = await _submissionService.Submit(new Submission
                {
                    ID = Guid.NewGuid().ToString(),
                    Status = false,
                    UserID = User.FindFirst(Constant.ID).Value,
                    Code = code,
                    Language = "cpp",
                    CreatedAt = DateTime.Now,
                    SubmissionDetails = new List<SubmissionDetail>()
                }, ID);
                return Ok(_mapper.Map<Submission, SubmissionDTO>(submission));
            }
            return BadRequest(new ErrorResponse
            {
                error = "Submit failed.",
                detail = "File empty."
            });
        }

        [HttpGet("{ID}/submissions")]
        [Authorize]
        public IActionResult GetSubmitOfProblem(string ID)
        {
            Problem problem = _problemService.FindByID(ID);
            if (problem == null)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Resource not found.",
                    detail = "Problem does not exist."
                });
            }
            return Ok(_mapper.Map<IEnumerable<Submission>, IEnumerable<SubmissionDTO>>(_submissionService.GetSubmissionsOfProblem(ID)));
        }

        [HttpPost("{ID}/reports")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Report(string ID, ReportInput input)
        {
            Problem problem = _problemService.FindByID(ID);
            if (problem == null)
            {
                return NotFound(new ErrorResponse
                {
                    error = "Resource not found.",
                    detail = "Problem does not exist."
                });
            }
            await _reportService.Add(new Report
            {
                ID = Guid.NewGuid().ToString(),
                Content = input.Content,
                Title = input.Title,
                ProblemID = ID,
                UserID = User.FindFirst(Constant.ID).Value,
                CreatedAt = DateTime.Now,
            });
            return Ok();
        }
    }
}
