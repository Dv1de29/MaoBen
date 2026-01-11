import { useState, useEffect, useRef } from "react";
import { useLocation } from "react-router-dom";
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { 
    faMagnifyingGlass, faPhone, faVideo, faEllipsisVertical, 
    faPaperclip, faImage, faPaperPlane, faFaceSmile 
} from "@fortawesome/free-solid-svg-icons";

import '../styles/ChatPage.css';

// --- 1. Types matching Backend DTOs ---

interface ConversationDto {
    otherUserId: string;
    otherUserUsername: string; // Used for API calls
    otherUserProfilePictureUrl: string;
    lastMessagePreview: string;
    lastMessageTime: string;
    unreadCount: number;
}

interface DirectMessageDto {
    id: number;
    content: string;
    createdAt: string;
    senderId: string;
    senderUsername: string;
    senderProfilePictureUrl: string;
    isMine: boolean;
}

function ChatPage() {
    const location = useLocation(); // To handle redirect from Profile Page

    // --- State ---
    const [conversations, setConversations] = useState<ConversationDto[]>([]);
    // We use USERNAME now because your backend API expects usernames for GET/POST
    const [activeChatUsername, setActiveChatUsername] = useState<string | null>(null);
    
    const [messages, setMessages] = useState<DirectMessageDto[]>([]);
    const [inputText, setInputText] = useState("");
    const [isLoadingMessages, setIsLoadingMessages] = useState(false);

    // Auto-scroll ref
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const connectionRef = useRef<HubConnection | null>(null);

    const activeConversation = conversations.find(c => c.otherUserUsername === activeChatUsername);

    
    useEffect(() => {
        if (!activeConversation) return;

        const newConnection = new HubConnectionBuilder()
                .withUrl("/directMessageHub", {
                    accessTokenFactory: () => sessionStorage.getItem("userToken") || ""
                })
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Information)
                .build();

        newConnection.start()
                .then(() => {
                    console.log("Connected to SignalR Hub");
                })
                .catch(err => console.error("SignalR Connection Error: ", err));

        newConnection.on("ReceiveDirectMessage", (message: DirectMessageDto) => {
            const isRelevant = message.senderUsername === activeChatUsername || message.isMine;
            
            if (isRelevant){
                setMessages(prev => {
                    if ( prev.some(m => m.id === message.id)) return prev;
                    return [...prev, message];
                })

                messagesEndRef.current?.scrollIntoView({ behavior: "smooth"});
            }
        });

        newConnection.on("MessageDeleted", ({ messageId }: {messageId: number}) => {
            setMessages(prev => prev.filter(m => m.id !== messageId));
        })

        connectionRef.current = newConnection;

        return () => {
            newConnection.stop();
        };

    }, [activeChatUsername]);


    useEffect(() => {
        const fetchConversations = async () => {
            const token = sessionStorage.getItem("userToken");
            if (!token) return;

            try {
                const res = await fetch('/api/DirectMessages/conversations', {
                    headers: { 'Authorization': `Bearer ${token}` },
                });

                if (!res.ok) throw new Error(`Failed to load conversations: ${res.status}, ${res.statusText}`);
                let data: ConversationDto[] = await res.json();
                
                // --- Handle "Start Chat" from Profile Page ---
                if (location.state?.targetUser) {
                    const target = location.state.targetUser; // { username, profilePictureUrl, ... }
                    
                    // Check if this user is already in our list
                    const exists = data.find(c => c.otherUserUsername === target.username);

                    if (exists) {
                        // If exists, just set them as active
                        setActiveChatUsername(target.username);
                    } else {
                        // If not, create a temporary conversation object so UI shows it
                        const newConv: ConversationDto = {
                            otherUserId: target.id || "temp_id",
                            otherUserUsername: target.username,
                            otherUserProfilePictureUrl: target.profilePictureUrl,
                            lastMessagePreview: "Start a conversation",
                            lastMessageTime: new Date().toISOString(),
                            unreadCount: 0
                        };
                        // Add to top of list
                        data = [newConv, ...data];
                        setActiveChatUsername(target.username);
                    }
                    // Clear history state so refresh doesn't trigger this again
                    window.history.replaceState({}, document.title);
                } else if (data.length > 0 && !activeChatUsername) {
                    // Default: Select first chat if nothing active
                    setActiveChatUsername(data[0].otherUserUsername);
                }

                setConversations(data);

            } catch (e) {
                console.error("Error loading conversations:", e);
            }
        };

        fetchConversations();
    }, [location.state]); // Re-run if location state changes (though mostly runs on mount)


    // --- 3. Fetch Messages when Active Chat Changes ---
    useEffect(() => {
        if (!activeChatUsername) return;

        const fetchMessages = async () => {
            setIsLoadingMessages(true);
            const token = sessionStorage.getItem("userToken");
            if (!token) return;

            try {
                // Using USERNAME in URL as requested
                const res = await fetch(`/api/DirectMessages/conversation/${activeChatUsername}`, {
                    headers: { 'Authorization': `Bearer ${token}` },
                });

                if (!res.ok) throw new Error("Failed to load messages");
                const data: DirectMessageDto[] = await res.json();
                
                setMessages(data);
            } catch (e) {
                console.error("Error loading messages:", e);
            } finally {
                setIsLoadingMessages(false);
            }
        };

        fetchMessages();
    }, [activeChatUsername]);

    // Auto-scroll
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [messages, isLoadingMessages]);


    // --- 4. Send Message Logic (Optimistic) ---
    const handleSendMessage = async () => {
        if (!inputText.trim() || !activeChatUsername) return;

        const textToSend = inputText;
        const tempId = Date.now(); // Temp ID for UI

        // 1. Optimistic Update
        const optimisticMsg: DirectMessageDto = {
            id: tempId,
            content: textToSend,
            createdAt: new Date().toISOString(),
            senderId: "me",
            senderUsername: "Me",
            senderProfilePictureUrl: "", // Current user pic (could get from context)
            isMine: true
        };
        
        setMessages(prev => [...prev, optimisticMsg]);
        setInputText(""); 

        const token = sessionStorage.getItem("userToken");

        try {
            // 2. API Call (Using Username)
            const res = await fetch(`/api/DirectMessages/send/${activeChatUsername}`, {
                method: "POST",
                headers: { 
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ content: textToSend })
            });

            if (!res.ok) {
                const errorData = await res.json();
                throw new Error(errorData.error || "Failed to send");
            }

            // 3. Success: Server returns the created message (with real ID)
            // Note: Depending on your Controller return, you might need to adjust this
            // If controller returns `CreatedAtAction`, `res.json()` is the body.
            // Assuming your controller returns { id, content, createdAt, ... } inside the body or a wrapper
            
            // Refetch or just keep optimistic (Simpler for now: keep optimistic, 
            // but ideally we swap ID here if we need to edit/delete later).
            
        } catch (e) {
            console.error("Error sending message:", e);
            alert("Failed to send message.");
            // 4. Rollback on error
            setMessages(prev => prev.filter(m => m.id !== tempId));
            setInputText(textToSend); 
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') handleSendMessage();
    };

    // --- Helpers ---
    const formatTime = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    };

    const formatTimeAgo = (dateString: string) => {
        const date = new Date(dateString);
        const now = new Date();
        const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
        
        if (diffInSeconds < 60) return "Just now";
        if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
        if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
        return `${Math.floor(diffInSeconds / 86400)}d ago`;
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
                            key={conv.otherUserUsername} 
                            className={`conversation-item ${activeChatUsername === conv.otherUserUsername ? 'active' : ''}`}
                            onClick={() => setActiveChatUsername(conv.otherUserUsername)}
                        >
                            <div className="avatar-wrapper">
                                <img src={conv.otherUserProfilePictureUrl || "/assets/img/no_user.png"} alt={conv.otherUserUsername} />
                            </div>
                            
                            <div className="conv-info">
                                <div className="conv-top">
                                    <span className="conv-name">{conv.otherUserUsername}</span>
                                    <span className="conv-time">{conv.lastMessageTime ? formatTimeAgo(conv.lastMessageTime) : ''}</span>
                                </div>
                                <div className="conv-bottom">
                                    <p className="conv-last-msg">{conv.lastMessagePreview}</p>
                                    {conv.unreadCount > 0 && (
                                        <span className="unread-badge">{conv.unreadCount}</span>
                                    )}
                                </div>
                            </div>
                        </div>
                    ))}
                    {conversations.length === 0 && <div className="no-conv-placeholder">No conversations yet</div>}
                </div>
            </aside>

            {/* --- RIGHT SIDE: ACTIVE CHAT --- */}
            <main className="chat-window">
                {activeChatUsername && activeConversation ? (
                    <>
                        {/* Chat Header */}
                        <header className="chat-header">
                            <div className="chat-user-profile">
                                <div className="avatar-wrapper small">
                                    <img src={activeConversation.otherUserProfilePictureUrl || "/assets/img/no_user.png"} alt="" />
                                </div>
                                <div className="chat-user-details">
                                    <h3>{activeConversation.otherUserUsername}</h3>
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
                            {isLoadingMessages && <div className="loading-msg">Loading messages...</div>}
                            
                            {messages.map((msg) => (
                                <div key={msg.id} className={`message-row ${msg.isMine ? 'me' : 'them'}`}>
                                    {!msg.isMine && (
                                        <img 
                                            src={msg.senderProfilePictureUrl || activeConversation.otherUserProfilePictureUrl || "/assets/img/no_user.png"} 
                                            alt="avatar" 
                                            className="msg-avatar" 
                                        />
                                    )}
                                    <div className="message-content">
                                        <div className="message-bubble">
                                            {msg.content}
                                        </div>
                                        <span className="message-time">{formatTime(msg.createdAt)}</span>
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
                    <div className="no-chat-selected">Select a conversation or start a new one</div>
                )}
            </main>
        </div>
    )
}

export default ChatPage;