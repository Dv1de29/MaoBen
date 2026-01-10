import { useState, useEffect, useRef } from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { 
    faMagnifyingGlass, 
    faPhone, 
    faVideo, 
    faEllipsisVertical, 
    faPaperclip, 
    faImage, 
    faPaperPlane, 
    faFaceSmile 
} from "@fortawesome/free-solid-svg-icons";

import '../styles/ChatPage.css';


import type { ConversationType } from "../assets/types";


// --- Types ---
interface User {
    id: string;
    name: string;
    image: string;
    isOnline: boolean;
}

interface Conversation {
    id: string;
    user: User;
    lastMessage: string;
    timeAgo: string;
    unreadCount: number;
}

interface Message {
    id: number;
    text: string;
    time: string;
    isMe: boolean; // True if I sent it, False if received
}

// --- Mock Data (Matching the Image) ---
const MOCK_CONVERSATIONS: Conversation[] = [
    {
        id: "1",
        user: { id: "u1", name: "Sarah Johnson", image: "https://i.pravatar.cc/150?u=1", isOnline: true },
        lastMessage: "That sounds great! Let's meet up tomorrow.",
        timeAgo: "2m ago",
        unreadCount: 2
    },
    {
        id: "2",
        user: { id: "u2", name: "Michael Chen", image: "https://i.pravatar.cc/150?u=2", isOnline: true },
        lastMessage: "Did you see the latest update?",
        timeAgo: "15m ago",
        unreadCount: 0
    },
    {
        id: "3",
        user: { id: "u3", name: "Emma Rodriguez", image: "https://i.pravatar.cc/150?u=3", isOnline: false },
        lastMessage: "Thanks for your help earlier!",
        timeAgo: "1h ago",
        unreadCount: 0
    },
    {
        id: "4",
        user: { id: "u4", name: "James Wilson", image: "https://i.pravatar.cc/150?u=4", isOnline: false },
        lastMessage: "Looking forward to the event!",
        timeAgo: "3h ago",
        unreadCount: 1
    },
    
];

// Mock messages for Sarah (Active Chat)
const INITIAL_MESSAGES: Message[] = [
    { id: 1, text: "Hey! How are you doing?", time: "1:25 PM", isMe: false },
    { id: 2, text: "I'm doing great, thanks! How about you?", time: "1:27 PM", isMe: true },
    { id: 3, text: "Pretty good! I wanted to talk to you about the project we discussed last week.", time: "1:28 PM", isMe: false },
];

function ChatPage() {
    // State for the list of users (conversations)
    const [conversations, setConversations] = useState<Conversation[]>(MOCK_CONVERSATIONS);
    
    // State for the currently selected chat
    const [activeChatId, setActiveChatId] = useState<string>("1");
    
    // State for messages in the active chat
    const [messages, setMessages] = useState<Message[]>(INITIAL_MESSAGES);
    const [inputText, setInputText] = useState("");

    // Auto-scroll to bottom ref
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const activeConversation = conversations.find(c => c.id === activeChatId);


    useEffect(() => {
        const token = sessionStorage.getItem("userToken");

        if ( !token ) return

        const fetchConv = async () => {
            try{
                const res = await fetch('/api/DirectMessages', {
                    method: "GET",
                    headers: { 'Authorization' : `Bearer ${token}` },
                })

                if ( !res.ok ){
                    throw new Error(`Response Error: ${res.status}, ${res.statusText}`);
                }

                const data = await res.json();


            }
            catch(e){
                console.error("Error at getting conv: ", e);
            }
        }
    }, []);



    // Auto-scroll to bottom when messages change
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [messages]);

    const handleSendMessage = () => {
        if (!inputText.trim()) return;

        const newMessage: Message = {
            id: Date.now(),
            text: inputText,
            time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
            isMe: true
        };

        setMessages([...messages, newMessage]);
        setInputText("");
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') handleSendMessage();
    };

    return (
        <div className="chat-layout">
            {/* --- LEFT SIDEBAR: CONVERSATIONS --- */}
            <aside className="chat-sidebar">
                <div className="sidebar-header">
                    <h2>Messages</h2>
                    <div className="search-bar">
                        <FontAwesomeIcon icon={faMagnifyingGlass} className="search-icon" />
                        <input type="text" placeholder="Search conversations..." />
                    </div>
                </div>

                <div className="conversation-list">
                    {conversations.map((conv) => (
                        <div 
                            key={conv.id} 
                            className={`conversation-item ${activeChatId === conv.id ? 'active' : ''}`}
                            onClick={() => setActiveChatId(conv.id)}
                        >
                            <div className="avatar-wrapper">
                                <img src={conv.user.image} alt={conv.user.name} />
                                {conv.user.isOnline && <span className="status-dot online"></span>}
                            </div>
                            
                            <div className="conv-info">
                                <div className="conv-top">
                                    <span className="conv-name">{conv.user.name}</span>
                                    <span className="conv-time">{conv.timeAgo}</span>
                                </div>
                                <div className="conv-bottom">
                                    <p className="conv-last-msg">{conv.lastMessage}</p>
                                    {conv.unreadCount > 0 && (
                                        <span className="unread-badge">{conv.unreadCount}</span>
                                    )}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            </aside>

            {/* --- RIGHT SIDE: ACTIVE CHAT --- */}
            <main className="chat-window">
                {activeConversation ? (
                    <>
                        {/* Chat Header */}
                        <header className="chat-header">
                            <div className="chat-user-profile">
                                <div className="avatar-wrapper small">
                                    <img src={activeConversation.user.image} alt="" />
                                    {activeConversation.user.isOnline && <span className="status-dot online"></span>}
                                </div>
                                <div className="chat-user-details">
                                    <h3>{activeConversation.user.name}</h3>
                                    <span className="status-text">{activeConversation.user.isOnline ? "Active now" : "Offline"}</span>
                                </div>
                            </div>
                            <div className="chat-header-actions">
                                <button className="icon-btn"><FontAwesomeIcon icon={faPhone} /></button>
                                <button className="icon-btn"><FontAwesomeIcon icon={faVideo} /></button>
                                <button className="icon-btn"><FontAwesomeIcon icon={faEllipsisVertical} /></button>
                            </div>
                        </header>

                        {/* Messages Area */}
                        <div className="messages-area">
                            {messages.map((msg) => (
                                <div key={msg.id} className={`message-row ${msg.isMe ? 'me' : 'them'}`}>
                                    {!msg.isMe && (
                                        <img src={activeConversation.user.image} alt="avatar" className="msg-avatar" />
                                    )}
                                    <div className="message-content">
                                        <div className="message-bubble">
                                            {msg.text}
                                        </div>
                                        <span className="message-time">{msg.time}</span>
                                    </div>
                                </div>
                            ))}
                            <div ref={messagesEndRef} />
                        </div>

                        {/* Input Area */}
                        <div className="chat-input-area">
                            <div className="input-actions-left">
                                <button className="icon-btn"><FontAwesomeIcon icon={faImage} /></button>
                                <button className="icon-btn"><FontAwesomeIcon icon={faPaperclip} /></button>
                            </div>
                            <div className="input-wrapper">
                                <input 
                                    type="text" 
                                    placeholder="Type a message..." 
                                    value={inputText}
                                    onChange={(e) => setInputText(e.target.value)}
                                    onKeyDown={handleKeyDown}
                                />
                                <FontAwesomeIcon icon={faFaceSmile} className="smiley-icon" />
                            </div>
                            <button className="send-btn" onClick={handleSendMessage}>
                                <FontAwesomeIcon icon={faPaperPlane} />
                            </button>
                        </div>
                    </>
                ) : (
                    <div className="no-chat-selected">Select a conversation to start chatting</div>
                )}
            </main>
        </div>
    )
}

export default ChatPage;