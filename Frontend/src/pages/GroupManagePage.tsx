import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
// Added faTriangleExclamation for the warning icon
import { faCheck, faXmark, faTrash, faSave, faTriangleExclamation } from '@fortawesome/free-solid-svg-icons';
import '../styles/GroupManagePage.css';

interface UserDto {
    username: string;
    name: string;
    profilePictureUrl?: string;
    role?: string;
}

interface GroupDetailsDto {
    id: number;
    name: string;
    description: string;
    ownerUsername: string;
    isUserMemeber: string;
}

function GroupManagePage() {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const groupId = Number(id);

    const [group, setGroup] = useState<GroupDetailsDto | null>(null);
    const [members, setMembers] = useState<UserDto[]>([]);
    const [requests, setRequests] = useState<UserDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [editForm, setEditForm] = useState({ name: '', description: '' });

    const myUsername = sessionStorage.getItem("userName");

    useEffect(() => {
        if (group && myUsername && group.ownerUsername !== myUsername) {
            navigate(-1);
        }
    }, [group, myUsername, navigate]);

    useEffect(() => {
        const fetchGroupData = async () => {
            const token = sessionStorage.getItem("userToken");
            if (!token) return;

            try {
                const resGroup = await fetch(`/api/Groups/${groupId}`, {
                    headers: { 'Authorization': `Bearer ${token}` }
                });
                if (resGroup.ok) {
                    const groupData: GroupDetailsDto = await resGroup.json();
                    setGroup(groupData);
                    setEditForm({ name: groupData.name, description: groupData.description });
                }

                const resMembers = await fetch(`/api/Groups/${groupId}/members`, {
                    headers: { 'Authorization': `Bearer ${token}` }
                });
                if (resMembers.ok) setMembers(await resMembers.json());

                const resReqs = await fetch(`/api/Groups/${groupId}/requests`, {
                    headers: { 'Authorization': `Bearer ${token}` }
                });
                if (resReqs.ok) setRequests(await resReqs.json());

            } catch (error) {
                console.error("Failed to load group data", error);
            } finally {
                setLoading(false);
            }
        };

        if (groupId) fetchGroupData();
    }, [groupId]);

    // --- HANDLERS ---

    const handleSaveGroup = async () => {
        const token = sessionStorage.getItem("userToken");
        try {
            const res = await fetch(`/api/Groups/edit/${groupId}`, {
                method: 'PUT',
                headers: { 
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(editForm)
            });

            if (res.ok) {
                alert("Group details updated successfully!");
                setGroup(prev => prev ? { ...prev, ...editForm } : null);
            } else {
                const err = await res.json();
                alert(err.error || "Failed to update group.");
            }
        } catch (e) {
            console.error("Error updating group", e);
        }
    }

    const handleAccept = async (username: string) => {
        const token = sessionStorage.getItem("userToken");
        try {
            const res = await fetch(`/api/Groups/${groupId}/accept/${username}`, {
                method: 'PUT',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                const user = requests.find(u => u.username === username);
                if (user) {
                    setMembers(prev => [...prev, user]);
                    setRequests(prev => prev.filter(u => u.username !== username));
                }
            }
        } catch (e) {
            console.error("Error accepting", e);
        }
    };

    const handleReject = async (username: string) => {
        const token = sessionStorage.getItem("userToken");
        try {
            const res = await fetch(`/api/Groups/${groupId}/reject/${username}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                setRequests(prev => prev.filter(u => u.username !== username));
            }
        } catch (e) {
            console.error("Error rejecting", e);
        }
    };

    const handleKick = async (username: string) => {
        const confirmDelete = window.confirm(`Are you sure you want to remove ${username} from the group?`);
        if (!confirmDelete) return;

        const token = sessionStorage.getItem("userToken");
        try {
            const res = await fetch(`/api/Groups/${groupId}/members/${username}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                setMembers(prev => prev.filter(u => u.username !== username));
            } else {
                alert("Failed to remove member.");
            }
        } catch (e) {
            console.error("Error kicking member", e);
        }
    };

    // NEW: Handle Delete Group
    const handleDeleteGroup = async () => {
        // Double confirmation for safety
        const confirmDelete = window.confirm("WARNING: This will permanently delete the group and remove all members. This action cannot be undone. Are you sure?");
        
        if (!confirmDelete) return;

        const token = sessionStorage.getItem("userToken");
        try {
            // Using your existing DELETE /api/Groups/{id} endpoint
            const res = await fetch(`/api/Groups/${groupId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                alert("Group deleted successfully.");
                navigate('/groups'); // Redirect to groups list or home page after deletion
            } else {
                const data = await res.json();
                alert(data.error || "Failed to delete group.");
            }
        } catch (e) {
            console.error("Error deleting group", e);
            alert("An error occurred while deleting the group.");
        }
    };

    if (loading) return <div className="manage-container">Loading...</div>;
    if (!group) return <div className="manage-container">Group not found.</div>;

    return (
        <div className="manage-container">
            {/* Header with Inputs */}
            <div className="manage-header">
                <input 
                    type="text" 
                    className="editable-input input-name"
                    value={editForm.name}
                    onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                    placeholder="Group Name"
                />
                <input 
                    type="text"
                    className="editable-input input-desc"
                    value={editForm.description}
                    onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                    placeholder="Group Description"
                />
                <button className="btn-save-changes" onClick={handleSaveGroup}>
                    <FontAwesomeIcon icon={faSave} /> Commit Saves
                </button>
            </div>

            <div className="manage-grid">
                {/* Pending Requests Column */}
                <div className="section">
                    <h2 className="section-header">Pending Requests ({requests.length})</h2>
                    {requests.length === 0 ? (
                        <p className="empty-state">No pending requests.</p>
                    ) : (
                        <ul className="manage-list">
                            {requests.map(user => (
                                <li key={user.username} className="manage-item">
                                    <div className="user-flex">
                                        <img 
                                            src={user.profilePictureUrl || "/assets/img/no_user.png"} 
                                            alt={user.username} 
                                            className="user-avatar"
                                        />
                                        <div className="user-details">
                                            <span className="user-name">{user.username}</span>
                                            <span className="user-role">{user.name}</span>
                                        </div>
                                    </div>
                                    <div className="actions-row">
                                        <button className="btn-icon btn-accept" onClick={() => handleAccept(user.username)}>
                                            <FontAwesomeIcon icon={faCheck} />
                                        </button>
                                        <button className="btn-icon btn-reject" onClick={() => handleReject(user.username)}>
                                            <FontAwesomeIcon icon={faXmark} />
                                        </button>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* Members Column */}
                <div className="section">
                    <h2 className="section-header">Members ({members.length})</h2>
                    {members.length === 0 ? (
                        <p className="empty-state">No members found.</p>
                    ) : (
                        <ul className="manage-list">
                            {members.map(user => (
                                <li key={user.username} className="manage-item">
                                    <div className="user-flex">
                                        <img 
                                            src={user.profilePictureUrl || "/assets/img/no_user.png"} 
                                            alt={user.username} 
                                            className="user-avatar"
                                        />
                                        <div className="user-details">
                                            <span className="user-name">{user.username}</span>
                                            <span className="user-role">
                                                {user.username === group.ownerUsername ? "Owner" : "Member"}
                                            </span>
                                        </div>
                                    </div>
                                    {user.username !== myUsername && (
                                        <div className="actions-row">
                                            <button className="btn-icon btn-kick" onClick={() => handleKick(user.username)}>
                                                <FontAwesomeIcon icon={faTrash} />
                                            </button>
                                        </div>
                                    )}
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            </div>

            {/* NEW: Danger Zone Footer */}
            <div className="danger-zone">
                <button className="btn-delete-group" onClick={handleDeleteGroup}>
                    <FontAwesomeIcon icon={faTriangleExclamation} /> Delete Group Permanently
                </button>
            </div>
        </div>
    );
}

export default GroupManagePage;