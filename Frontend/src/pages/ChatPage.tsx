import { useState, useEffect, useRef } from "react";
import { useLocation } from "react-router-dom";
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { 
    faMagnifyingGlass, faPhone, faVideo, faEllipsisVertical, 
    faPaperclip, faImage, faPaperPlane, faFaceSmile, faUsers, faPlus, faTimes, faTrash 
} from "@fortawesome/free-solid-svg-icons";

import '../styles/ChatPage.css';

// --- 1. DTO Types (From Backend) ---

interface ConversationDto {
    otherUserId: string;
    otherUserUsername: string;
    otherUserProfilePictureUrl: string;
    lastMessagePreview: string;
    lastMessageTime: string;
    unreadCount: number;
}

interface GroupDto {
    id: number;
    name: string;
    description: string;
    ownerUsername: string;
    isUserMember: boolean;
}

interface MessageDto {
    id: number;
    content: string;
    createdAt: string;
    // Common fields
    senderUsername?: string; 
    username?: string; // Group DTO uses 'username' instead of senderUsername
    senderProfilePictureUrl?: string;
    profilePictureUrl?: string; // Group DTO might use this
    isMine: boolean;
}

// --- 2. Unified UI Type (To handle both in one list) ---
type ChatType = 'private' | 'group';

interface ChatSession {
    id: string | number; // Username (for DM) or GroupID (for Groups)
    displayId: string | number; // Helper for API calls
    name: string;
    avatarUrl: string;
    lastMessage: string;
    timestamp: string;
    type: ChatType;
    unreadCount: number;
    ownerUsername?: string;
}

function ChatPage() {
    const location = useLocation();

    // --- State ---
    const [chatList, setChatList] = useState<ChatSession[]>([]);
    const [activeChat, setActiveChat] = useState<ChatSession | null>(null);
    const [messages, setMessages] = useState<MessageDto[]>([]);
    const [inputText, setInputText] = useState("");
    const [isLoadingMessages, setIsLoadingMessages] = useState(false);

    const [currentUsername, setCurrentUsername] = useState<string>("");
    const [isAdmin, setIsAdmin] = useState<boolean>(false);


    const [isGroupModalOpen, setIsGroupModalOpen] = useState(false);
    const [newGroupName, setNewGroupName] = useState("");
    const [newGroupDesc, setNewGroupDesc] = useState("");
    const [isCreatingGroup, setIsCreatingGroup] = useState(false);

    // Refs
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const connectionRef = useRef<HubConnection | null>(null);

    useEffect(() => {
        const adminRole = sessionStorage.getItem("userRole") === "Admin";
        const username: string = sessionStorage.getItem("userName") || "";
        setCurrentUsername(username);
        setIsAdmin(adminRole);
    }, []);

    // --- 1. Fetch ALL Chats (Conversations + Groups) ---
    useEffect(() => {
        const fetchAllChats = async () => {
            const token = sessionStorage.getItem("userToken");
            if (!token) return;

            try {
                // Parallel Fetch
                const [convRes, groupRes] = await Promise.all([
                    fetch('/api/DirectMessages/conversations', { headers: { 'Authorization': `Bearer ${token}` } }),
                    fetch('/api/Groups', { headers: { 'Authorization': `Bearer ${token}` } })
                ]);

                let unifiedList: ChatSession[] = [];

                // A. Process Direct Messages
                if (convRes.ok) {
                    const convData: ConversationDto[] = await convRes.json();
                    const dms: ChatSession[] = convData.map(c => ({
                        id: c.otherUserUsername, // Unique ID for DMs is username
                        displayId: c.otherUserUsername,
                        name: c.otherUserUsername,
                        avatarUrl: c.otherUserProfilePictureUrl,
                        lastMessage: c.lastMessagePreview,
                        timestamp: c.lastMessageTime,
                        type: 'private',
                        unreadCount: c.unreadCount
                    }));
                    unifiedList = [...unifiedList, ...dms];
                }

                // B. Process Groups
                if (groupRes.ok) {
                    const groupData: GroupDto[] = await groupRes.json();
                    // Filter only groups where user is a member (if API returns all)
                    const myGroups = groupData.filter(g => g.isUserMember);
                    
                    const groups: ChatSession[] = myGroups.map(g => ({
                        id: g.id, // Unique ID for Groups is Int ID
                        displayId: g.id,
                        name: g.name,
                        avatarUrl: "", // Default group icon logic later
                        lastMessage: "Group Chat", // You might want to fetch last message for groups too
                        timestamp: new Date().toISOString(), // Placeholder if API doesn't send time
                        type: 'group',
                        unreadCount: 0,
                        ownerUsername: g.ownerUsername,
                    }));
                    unifiedList = [...unifiedList, ...groups];
                }

                // Sort by latest activity
                unifiedList.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());

                // --- Handle Redirect from Profile (Start Chat) ---
                if (location.state?.targetUser) {
                    const target = location.state.targetUser;
                    const exists = unifiedList.find(c => c.type === 'private' && c.id === target.username);
                    
                    if (exists) {
                        setActiveChat(exists);
                    } else {
                        // Create temp chat
                        const newChat: ChatSession = {
                            id: target.username,
                            displayId: target.username,
                            name: target.username,
                            avatarUrl: target.profilePictureUrl,
                            lastMessage: "Start a conversation",
                            timestamp: new Date().toISOString(),
                            type: 'private',
                            unreadCount: 0
                        };
                        unifiedList = [newChat, ...unifiedList];
                        setActiveChat(newChat);
                    }
                    window.history.replaceState({}, document.title);
                } 
                // Default select first
                else if (unifiedList.length > 0 && !activeChat) {
                    setActiveChat(unifiedList[0]);
                }

                setChatList(unifiedList);

            } catch (e) {
                console.error("Error loading chats:", e);
            }
        };

        fetchAllChats();
    }, [location.state]);


    // --- 2. SignalR Connection Logic (Dynamic Join) ---
    useEffect(() => {
        if (!activeChat) return;

        const newConnection = new HubConnectionBuilder()
            .withUrl("http://localhost:5000/directMessageHub", {
                accessTokenFactory: () => sessionStorage.getItem("userToken") || ""
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        const initSignalR = async () => {
            try {
                await newConnection.start();
                console.log("✅ SignalR Connected");

                // --- DYNAMIC JOIN LOGIC ---
                if (activeChat.type === 'private') {
                    // For DM: Send Username
                    await newConnection.invoke("JoinChat", activeChat.id as string);
                    console.log(`Joined Private Chat: ${activeChat.id}`);
                } 
                else if (activeChat.type === 'group') {
                    // For Group: Send Group ID (int)
                    await newConnection.invoke("JoinGroupChat", activeChat.id as number);
                    console.log(`Joined Group Chat: ${activeChat.id}`);
                }

            } catch (err) {
                console.error("SignalR Error: ", err);
            }
        };

        // --- LISTENERS ---
        
        // 1. Direct Message Listener
        newConnection.on("ReceiveDirectMessage", (message: MessageDto) => {
            if (activeChat.type === 'private' && (message.senderUsername === activeChat.id || message.isMine)) {
                addMessageToState(message);
            }
        });

        // 2. Group Message Listener (NEW)
        newConnection.on("ReceiveGroupMessage", (message: MessageDto) => {
            // Check if this message belongs to the active group
            // (message doesn't have groupId usually, relying on connection room)
            if (activeChat.type === 'group') {
                addMessageToState(message);
            }
        });

        // 3. Deletion Listeners
        newConnection.on("MessageDeleted", ({ messageId }) => removeMessageFromState(messageId));
        newConnection.on("GroupMessageDeleted", ({ messageId }) => removeMessageFromState(messageId));

        initSignalR();
        connectionRef.current = newConnection;

        return () => {
            newConnection.stop();
        };

    }, [activeChat]); // Re-connects when switching chats (simplest approach)


    // --- 3. Helpers for State ---
    const addMessageToState = (msg: MessageDto) => {
        setMessages(prev => {
            if (prev.some(m => m.id === msg.id)) return prev;
            return [...prev, msg];
        });
    };

    const removeMessageFromState = (id: number) => {
        setMessages(prev => prev.filter(m => m.id !== id));
    };


    // --- 4. Fetch Messages for Active Chat ---
    useEffect(() => {
        if (!activeChat) return;

        const fetchMessages = async () => {
            setIsLoadingMessages(true);
            const token = sessionStorage.getItem("userToken");
            if (!token) return;

            try {
                let url = "";
                if (activeChat.type === 'private') {
                    url = `/api/DirectMessages/conversation/${activeChat.id}`;
                } else {
                    url = `/api/Groups/${activeChat.id}/messages`;
                }

                const res = await fetch(url, {
                    headers: { 'Authorization': `Bearer ${token}` },
                });

                if (!res.ok) throw new Error("Failed to load messages");
                const data: MessageDto[] = await res.json();
                setMessages(data);

            } catch (e) {
                console.error("Error loading messages:", e);
            } finally {
                setIsLoadingMessages(false);
            }
        };

        fetchMessages();
    }, [activeChat]);

    // Auto-scroll
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [messages]);


    // --- 5. Send Message ---
    const handleSendMessage = async () => {
        if (!inputText.trim() || !activeChat) return;

        const textToSend = inputText;
        const tempId = Date.now();
        const token = sessionStorage.getItem("userToken");

        // Optimistic UI
        const optimisticMsg: MessageDto = {
            id: tempId,
            content: textToSend,
            createdAt: new Date().toISOString(),
            isMine: true,
            // Group messages use 'username', DM use 'senderUsername'. Hack to show locally:
            senderUsername: "Me", 
            username: "Me" 
        };
        
        setMessages(prev => [...prev, optimisticMsg]);
        setInputText(""); 

        try {
            let url = "";
            
            if (activeChat.type === 'private') {
                url = `/api/DirectMessages/send/${activeChat.id}`;
            } else {
                url = `/api/Groups/${activeChat.id}/messages`;
            }

            const res = await fetch(url, {
                method: "POST",
                headers: { 
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ content: textToSend })
            });

            if (!res.ok) throw new Error("Failed to send");

            // SignalR will update the real message, we rely on duplicate check in addMessageToState

        } catch (e) {
            console.error("Send error:", e);
            setMessages(prev => prev.filter(m => m.id !== tempId)); // Rollback
            setInputText(textToSend);
            alert("Failed to send message");
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') handleSendMessage();
    };

    const handlePlusSignPress = () => {
        setIsGroupModalOpen(true);
    };

    const handleCreateGroup = async () => {
        if (!newGroupName.trim() || !newGroupDesc.trim()) {
            alert("Name and description are required.");
            return;
        }

        setIsCreatingGroup(true);
        const token = sessionStorage.getItem("userToken");

        try {
            const res = await fetch('/api/Groups', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    name: newGroupName,
                    description: newGroupDesc
                })
            });

            if (!res.ok) throw new Error("Failed to create group");
            
            const data = await res.json(); // Usually returns { message, groupId }

            // Create a temp chat object to add to the list immediately
            const newGroupChat: ChatSession = {
                id: data.groupId,
                displayId: data.groupId,
                name: newGroupName,
                avatarUrl: "",
                lastMessage: "Group created",
                timestamp: new Date().toISOString(),
                type: 'group',
                unreadCount: 0
            };

            // Add to top of list and close modal
            setChatList(prev => [newGroupChat, ...prev]);
            setActiveChat(newGroupChat);
            setIsGroupModalOpen(false);
            
            // Reset form
            setNewGroupName("");
            setNewGroupDesc("");

        } catch (e) {
            console.error(e);
            alert("Error creating group");
        } finally {
            setIsCreatingGroup(false);
        }
    };

    const handleDeleteGroup = async (e: React.MouseEvent, groupId: number) => {
        e.stopPropagation(); // Prevent opening the chat when clicking delete
        
        if (!window.confirm("Are you sure you want to delete this group? This cannot be undone.")) return;

        const token = sessionStorage.getItem("userToken");
        try {
            const res = await fetch(`/api/Groups/${groupId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                // Remove from UI immediately
                setChatList(prev => prev.filter(c => !(c.type === 'group' && c.id === groupId)));
                if (activeChat?.id === groupId) setActiveChat(null);
            } else {
                const err = await res.json();
                alert(err.error || "Failed to delete group");
            }
        } catch (error) {
            console.error("Delete error", error);
        }
    };


    // --- RENDER ---
    return (
        <div className="chat-layout">
            <aside className="chat-sidebar">
                <div className="sidebar-header">
                    <h2>Messages</h2>
                    {/* ... Search ... */}
                </div>

                <div className="conversation-list">
                    {chatList.map((chat) => (
                        <div 
                            key={`${chat.type}-${chat.id}`} 
                            className={`conversation-item ${activeChat?.id === chat.id && activeChat?.type === chat.type ? 'active' : ''}`}
                            onClick={() => setActiveChat(chat)}
                        >
                            <div className="avatar-wrapper">
                                {chat.type === 'group' ? (
                                    <div className="group-avatar-placeholder">
                                         <FontAwesomeIcon icon={faUsers} />
                                    </div>
                                ) : (
                                    <img src={chat.avatarUrl || "/assets/img/no_user.png"} alt={chat.name} />
                                )}
                            </div>
                            
                            <div className="conv-info">
                                <div className="conv-top">
                                    <span className="conv-name">{chat.name}</span>
                                    <span className="conv-time">{formatTimeAgo(chat.timestamp)}</span>
                                </div>
                                <div className="conv-bottom">
                                    <p className="conv-last-msg">
                                        {chat.type === 'group' ? <span className="group-tag">Group</span> : null}
                                        {chat.lastMessage}
                                    </p>
                                    {chat.unreadCount > 0 && <span className="unread-badge">{chat.unreadCount}</span>}
                                </div>
                            </div>
                            {chat.type === 'group' && (currentUsername === chat.ownerUsername || isAdmin) && (
                                <button 
                                    className="delete-chat-btn"
                                    onClick={(e) => handleDeleteGroup(e, chat.id as number)}
                                    title="Delete Group"
                                >
                                    <FontAwesomeIcon icon={faTrash} />
                                </button>
                            )}
                        </div>
                    ))}
                </div>
                <button 
                    className="new-chat-floating-btn"
                    onClick={() => handlePlusSignPress()}
                >
                    <FontAwesomeIcon icon={faPlus} />
                </button>
            </aside>

            <main className="chat-window">
                {activeChat ? (
                    <>
                        <header className="chat-header">
                            <div className="chat-user-profile">
                                <div className="avatar-wrapper small">
                                    {activeChat.type === 'group' ? (
                                        <div className="group-avatar-placeholder small">
                                             <FontAwesomeIcon icon={faUsers} />
                                        </div>
                                    ) : (
                                        <img src={activeChat.avatarUrl || "/assets/img/no_user.png"} alt="" />
                                    )}
                                </div>
                                <div className="chat-user-details">
                                    <h3>{activeChat.name}</h3>
                                    {activeChat.type === 'group' && <span className="status-text">Group Chat</span>}
                                </div>
                            </div>
                            <div className="chat-header-actions">
                                <button className="icon-btn"><FontAwesomeIcon icon={faEllipsisVertical} /></button>
                            </div>
                        </header>

                        <div className="messages-area">
                            {isLoadingMessages && <div className="loading-msg">Loading...</div>}
                            
                            {messages.map((msg) => (
                                <div key={msg.id} className={`message-row ${msg.isMine ? 'me' : 'them'}`}>
                                    {!msg.isMine && (
                                        <div className="msg-sender-info">
                                            {/* Show Avatar */}
                                            <img 
                                                src={msg.senderProfilePictureUrl || msg.profilePictureUrl || "/assets/img/no_user.png"} 
                                                alt="avatar" 
                                                className="msg-avatar" 
                                            />
                                        </div>
                                    )}
                                    <div className="message-content">
                                        {/* In groups, show the name of the person talking */}
                                        {activeChat.type === 'group' && !msg.isMine && (
                                            <span className="msg-sender-name">{msg.senderUsername || msg.username}</span>
                                        )}

                                        <div className="message-bubble">{msg.content}</div>
                                        <span className="message-time">{formatTime(msg.createdAt)}</span>
                                    </div>
                                </div>
                            ))}
                            <div ref={messagesEndRef} />
                        </div>

                        <div className="chat-input-area">
                           {/* ... Same Input Area ... */}
                           <input 
                                type="text" 
                                placeholder={activeChat.type === 'group' ? "Message group..." : "Type a message..."}
                                value={inputText}
                                onChange={(e) => setInputText(e.target.value)}
                                onKeyDown={handleKeyDown}
                            />
                            <button className="send-btn" onClick={handleSendMessage}>
                                <FontAwesomeIcon icon={faPaperPlane} />
                            </button>
                        </div>
                    </>
                ) : (
                    <div className="no-chat-selected">Select a conversation</div>
                )}
            </main>

            {/* --- MODAL OVERLAY --- */}
            {isGroupModalOpen && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h3>Create New Group</h3>
                            <button className="close-btn" onClick={() => setIsGroupModalOpen(false)}>
                                <FontAwesomeIcon icon={faTimes} />
                            </button>
                        </div>
                        <div className="modal-body">
                            <div className="form-group">
                                <label>Group Name</label>
                                <input 
                                    type="text" 
                                    placeholder="Ex: Weekend Trip" 
                                    value={newGroupName}
                                    onChange={e => setNewGroupName(e.target.value)}
                                />
                            </div>
                            <div className="form-group">
                                <label>Description</label>
                                <textarea 
                                    placeholder="What is this group about?" 
                                    value={newGroupDesc}
                                    onChange={e => setNewGroupDesc(e.target.value)}
                                />
                            </div>
                        </div>
                        <div className="modal-footer">
                            <button 
                                className="cancel-btn" 
                                onClick={() => setIsGroupModalOpen(false)}
                            >
                                Cancel
                            </button>
                            <button 
                                className="create-btn" 
                                onClick={handleCreateGroup}
                                disabled={isCreatingGroup}
                            >
                                {isCreatingGroup ? "Creating..." : "Create Group"}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}

// Helpers
const formatTime = (dateString: string) => {
    if(!dateString) return "";
    const date = new Date(dateString);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
};

const formatTimeAgo = (dateString: string) => {
    if(!dateString) return "";
    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
    
    if (diffInSeconds < 60) return "Just now";
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h`;
    return `${Math.floor(diffInSeconds / 86400)}d`;
};

export default ChatPage;