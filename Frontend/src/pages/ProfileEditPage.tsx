import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import type { UserProfileApiType, UserSettingsType } from '../assets/types'; // Adjust path as needed


import '../styles/ProfileEditPage.css'

const INITIAL_USER: UserSettingsType = {
    userName: "",
    name: "",
    description: "",
    profile_image: "/assets/img/no_user.jpg",
    privacy: false,
};

const EditProfilePage = () => {
    const navigate = useNavigate();

    const [formData, setFormData] = useState<Omit<UserSettingsType, 'id'>>({
        userName: INITIAL_USER.userName,
        name: INITIAL_USER.name,
        description: INITIAL_USER.description,
        profile_image: INITIAL_USER.profile_image,
        privacy: false,
    });

    const [uploading, setUploading] = useState(false);
    const originalImageUrlRef = useRef(INITIAL_USER.profile_image);

    const fileInputRef = useRef<HTMLInputElement>(null);


    //fetchUser
    useEffect(() => {
        const fetchUser = async () => {
            try{
                const token = sessionStorage.getItem("userToken");

                const res = await fetch(`http://localhost:5000/api/Profile`, {
                    method: "GET",
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    }
                });

                if ( !res.ok ){
                    throw new Error(`Fetching error: ${res.status}, ${res.statusText}`);
                }

                const data: UserProfileApiType = await res.json();

                const profileImgPath = data.profilePictureUrl ? data.profilePictureUrl : '/assets/img/no_user.png';

                originalImageUrlRef.current = profileImgPath;

                setFormData({
                    userName: data.username,
                    name: "Test Name",
                    description: data.description,
                    profile_image: profileImgPath,
                    privacy: data.privacy,
                });

            } catch(e){
                console.error("Error fetching: ", e);
            }
        }

        fetchUser();
    }, [])


    /// Change formular 
    const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        const { name, value } = e.target;
        setFormData(prev => ({
            ...prev,
            [name]: value
        }));
    };

    /// Import an image
    const handleImageClick = () => {
        if ( !uploading && fileInputRef.current ){
            fileInputRef.current.click();
        }
    }

    const handleFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if ( !file || !file.type.startsWith('image/')) {
            console.log("NO FILE")
            if ( e.target.value ) e.target.value = "";
            return;
        }

        const previewUrl = URL.createObjectURL(file);

        console.log(previewUrl)

        setFormData(prev => {
            return {
                ...prev,
                profile_image: previewUrl,
            }
        })

        setUploading(true);

        const formDataPayload = new FormData();
        formDataPayload.append('image_path', file)

        try{
            const token = sessionStorage.getItem("userToken");
            const res = await fetch("http://localhost:5000/api/Profile/upload_image", {
                method: "POST",
                headers: {
                    'Authorization': `Bearer ${token}`
                },
                body: formDataPayload
            });

            if ( !res.ok ){
                throw new Error(`Error at response: ${res.status}, ${res.statusText}`)
            }

            const result: { filePath: string } = await res.json();

            setFormData(prev => ({ ...prev, profile_image: result.filePath }));

            console.log("SUCCEDED WIHT PATH", result);

            URL.revokeObjectURL(previewUrl)
        } catch(e){
            console.error("Error at fetching: ", e);
            setFormData(prev => ({ ...prev, profile_image: originalImageUrlRef.current }));
            URL.revokeObjectURL(previewUrl);
        } finally{
            setUploading(false);
            if (e.target.value) e.target.value = '';
        }
    }

    const handleSubmit = (e: FormEvent) => {
        e.preventDefault();
        
        const UpdateUser = async () => {
            try{
                const token = sessionStorage.getItem("userToken");

                const res = await fetch(`http://localhost:5000/api/Profile`, {
                    method: "PUT",
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        privacy: formData.privacy,
                        description: formData.description,
                        profilePictureUrl: formData.profile_image,
                    })
                });

                if ( !res.ok ){
                    throw new Error(`Fetching error: ${res.status}, ${res.statusText}`);
                }

                navigate(-1); 

            } catch(e){
                console.error("Error fetching: ", e);
            }
        }

        UpdateUser()
    };

    return (
        <div className="edit-profile-container">
            <form className="edit-card" onSubmit={handleSubmit}>
                <h2>Edit Profile</h2>

                {/* Image Section */}
                <div className="image-section" onClick={handleImageClick}>
                    <img 
                        src={formData.profile_image} 
                        alt="Preview" 
                        className="preview-img"
                    />  
                    <input
                        type="file"
                        accept="image/*"
                        ref={fileInputRef}
                        onChange={handleFileChange}
                        style={{ display: 'none' }}
                        disabled={uploading}
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

                {/* Privacy section */}
                <div className="input-group">
                    {/* <label>Privacy</label> */}
                    <span>Privacy</span>
                    <div className="privacy-wrapper">
                        <div 
                            className={`privacy-choice ${formData.privacy ? "active" : ""} private`}
                            onClick={() => {
                                setFormData(prev => {
                                    return {
                                        ...prev,
                                        privacy: true,
                                    }
                                })
                            }}
                        >{"Private"}</div>
                        <div 
                            className={`privacy-choice ${formData.privacy ? "" : "active"} public`}
                            onClick={() => {
                                setFormData(prev => {
                                    return {
                                        ...prev,
                                        privacy: false,
                                    }
                                })
                            }}
                        >{"Public"}</div>
                    </div>
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