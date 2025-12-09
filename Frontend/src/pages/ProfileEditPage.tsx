import { useState, type ChangeEvent, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import type { UserSettingsType } from '../assets/types'; // Adjust path as needed


import '../styles/ProfileEditPage.css'

// Mock initial data (In a real app, you would fetch this via useEffect or Context)
const INITIAL_USER: UserSettingsType = {
    id: 123,
    userName: "coding_wizard",
    name: "John Doe",
    description: "Full stack developer | Coffee enthusiast",
    profile_image: "/assets/img/download.jpg"
};

const EditProfilePage = () => {
    const navigate = useNavigate();

    // We exclude 'id' from the state since we don't edit it
    const [formData, setFormData] = useState<Omit<UserSettingsType, 'id'>>({
        userName: INITIAL_USER.userName,
        name: INITIAL_USER.name,
        description: INITIAL_USER.description,
        profile_image: INITIAL_USER.profile_image
    });

    // Handle text and text area changes
    const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        const { name, value } = e.target;
        setFormData(prev => ({
            ...prev,
            [name]: value
        }));
    };

    // Handle Form Submit
    const handleSubmit = (e: FormEvent) => {
        e.preventDefault();
        console.log("Saving data:", { id: INITIAL_USER.id, ...formData });
        
        // TODO: Add your API call here (e.g., axios.put('/api/user', formData))
        
        // After save, go back to profile
        navigate(-1); 
    };

    return (
        <div className="edit-profile-container">
            <form className="edit-card" onSubmit={handleSubmit}>
                <h2>Edit Profile</h2>

                {/* Image Section */}
                <div className="image-section">
                    <img 
                        src={formData.profile_image} 
                        alt="Preview" 
                        className="preview-img"
                    />  
                </div>

                {/* Name Section */}
                <div className="input-group">
                    <label>Display Name</label>
                    <input 
                        type="text" 
                        name="name"
                        value={formData.name}
                        onChange={handleChange}
                    />
                </div>

                {/* Username Section */}
                <div className="input-group">
                    <label>Username</label>
                    <input 
                        type="text" 
                        name="userName"
                        value={formData.userName}
                        onChange={handleChange}
                    />
                </div>

                {/* Description Section */}
                <div className="input-group">
                    <label>Bio / Description</label>
                    <textarea 
                        name="description"
                        rows={4}
                        value={formData.description}
                        onChange={handleChange}
                    />
                </div>

                {/* Actions */}
                <div className="button-group">
                    <button 
                        type="button" 
                        className="btn-cancel" 
                        onClick={() => navigate(-1)}
                    >
                        Cancel
                    </button>
                    <button type="submit" className="btn-save">
                        Save Changes
                    </button>
                </div>
            </form>
        </div>
    );
};

export default EditProfilePage;