export interface UserSettingsType{
    userName: string,
    name: string,
    description: string,
    profile_image: string,
    privacy: boolean,
}





export interface UserProfileType{
    username: string,
    email: string,
    description: string
    profilePictureUrl: string,
    privacy: boolean,
    posts: PostType[],
}

export interface UserProfileApiType{
    username: string,
    email: string,
    profilePictureUrl: string,
    privacy: boolean,
    description: string,
}


// Define this alongside PostType or in a dedicated types file

export interface PostType{
    id: number,
    owner: string,
    img_path: string,
    nr_likes: number,
    has_liked: boolean,
    nr_comm: number,
}

export interface PostApiType {
    id: number;
    owner: string;
    image_path: string;
    nr_likes: number;
    nr_comms: number; 
}