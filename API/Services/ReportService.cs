﻿using API.Models.Entity;
using API.Repository;
using CodeStudy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IReplyRepository _replyRepository;
        private readonly IUnitOfWork _unitOfWork;
        public ReportService(IReportRepository reportRepository, IReplyRepository replyRepository, IUnitOfWork unitOfWork)
        {
            _reportRepository = reportRepository;
            _replyRepository = replyRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Add(ReportInput input, string authorID, string problemID)
        {
            Report report = new Report
            {
                ID = Guid.NewGuid().ToString(),
                Content = input.Content,
                Title = input.Title,
                ProblemID = problemID,
                UserID = authorID,
                CreatedAt = DateTime.Now,
            };
            await _reportRepository.AddAsync(report);
            await _unitOfWork.CommitAsync();
        }

        public Report FindByID(string ID)
        {
            return _reportRepository.GetReportsDetailSingle(x => x.ID == ID);
        }

        public IEnumerable<Report> GetAll()
        {
            return _reportRepository.GetReportsDetail();
        }

        public IEnumerable<Report> GetReportsOfUser(string userID)
        {
            return _reportRepository.GetReportsDetail(x => x.UserID == userID);
        }

        public async Task Remove(Report report)
        {
            _reportRepository.Remove(report);
            await _unitOfWork.CommitAsync();
        }

        public async Task Update(Report report, ReportInput input)
        {
            report.Title = input.Title;
            report.Content = input.Content;
            report.UpdatedAt = DateTime.Now;
            _reportRepository.Update(report);
            await _unitOfWork.CommitAsync();
        }

        public async Task<bool> Reply(Report report, ReplyInput input)
        {
            if (report.Reply == null)
            {
                report.Reply = new Reply
                {
                    Content = input.Content,
                    CreatedAt = DateTime.Now,
                };
                _reportRepository.Update(report);
                await _unitOfWork.CommitAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateReply(Report report, ReplyInput input)
        {
            if (report.Reply == null)
            {
                return false;
            }
            else
            {
                report.Reply.Content = input.Content;
                report.Reply.UpdatedAt = DateTime.Now;
                _reportRepository.Update(report);
                await _unitOfWork.CommitAsync();
                return true;
            }
        }

        public async Task<bool> RemoveReply(Report report)
        {
            if (report.Reply == null)
            {
                return false;
            }
            else
            {
                _replyRepository.Remove(report.Reply);
                await _unitOfWork.CommitAsync();
                return true;
            }
        }
    }
}
