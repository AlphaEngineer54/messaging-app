﻿using MessageService.Models;
using MessageService.Models.DTO.Conversation;
using MessageService.Models.DTO.Message;
using MessageService.Services;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations;

namespace MessageService.Hubs
{
    /// <summary>
    /// SignalR Hub that enables real-time chat messaging between clients.
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly MsgService _messageService;
        private readonly ConversationService _conversationService;

        public ChatHub(MsgService messageService, ConversationService conversationService)
        {
            _messageService = messageService;
            _conversationService = conversationService;
        }

        /// <summary>
        /// Broadcast a message to all connected clients.
        /// </summary>
        public async Task SendMessage(NewMessageDTO newMessage)
        {
            if (await ValidateAndRejectIfInvalid(newMessage)) return;

            var createdMessage = await CreateAndPersistMessageAsync(newMessage);
            await Clients.All.SendAsync("ReceiveMessage", createdMessage);
        }

        /// <summary>
        /// Send a message directly to a specific user.
        /// </summary>
        public async Task SendMessageToUser(NewMessageDTO newMessage)
        {
            if (await ValidateAndRejectIfInvalid(newMessage)) return;

            var createdMessage = await CreateAndPersistMessageAsync(newMessage);
            await Clients.User(createdMessage.ReceiverId.ToString())
                         .SendAsync("ReceiveMessage", createdMessage);
        }

        /// <summary>
        /// Add a user to a SignalR group representing a conversation.
        /// </summary>
        public async Task JoinGroup(JoinConversationDTO newUser)
        {
            if (await ValidateAndRejectIfInvalid(newUser)) return;

            var existingConversation = await _conversationService.GetConversationByIdAsync(newUser.ConversationId);
            if (existingConversation == null) return;

            existingConversation.Users.Add(new UserConversation
            {
                UserId = newUser.UserId,
                ConversationId = newUser.ConversationId
            });

            await _conversationService.UpdateConversationWithoutDTOAsync(existingConversation);
            await Groups.AddToGroupAsync(Context.ConnectionId, newUser.ConversationId.ToString());
        }

        /// <summary>
        /// Send a message to all members of a SignalR group (conversation).
        /// </summary>
        public async Task SendMessageToGroup(NewMessageDTO newMessage)
        {
            if (await ValidateAndRejectIfInvalid(newMessage)) return;

            var createdMessage = await CreateAndPersistMessageAsync(newMessage);
            await Clients.Group(newMessage.ConversationId.ToString())
                         .SendAsync("ReceiveMessage", createdMessage);
        }

        /// <summary>
        /// Validate an object and send validation errors to the caller if invalid.
        /// </summary>
        private async Task<bool> ValidateAndRejectIfInvalid(object model)
        {
            IList<string> errors = ValidateModel(model);
            if (errors.Count > 0)
            {
                await Clients.Caller.SendAsync("ValidationError", errors);
                // Optional logging:
                // _logger.LogWarning("Validation failed: {@Errors}", errors);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Validate a DTO object using DataAnnotations.
        /// </summary>
        private IList<string> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);

            return validationResults.Select(vr => vr.ErrorMessage!).ToList();
        }

        /// <summary>
        /// Convert and persist a DTO as a Message entity in the database.
        /// </summary>
        private async Task<Message> CreateAndPersistMessageAsync(NewMessageDTO dto)
        {
            var message = new Message
            {
                Content = dto.Content,
                ReceiverId = dto.ReceiverId,
                SenderId = dto.SenderId,
                ConversationId = dto.ConversationId,
                Date = DateTime.Now,
                Status = dto.Status
            };

            return await _messageService.CreateMessageAsync(message);
        }
    }
}
