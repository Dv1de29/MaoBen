import { useCallback, useEffect, useState } from 'react';

import '../styles/SearchDrawer.css'

import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faCircleXmark, faXmark } from '@fortawesome/free-solid-svg-icons';

interface SearchDrawerProps {
    isOpen: boolean;
}

function SearchDrawer({ isOpen }: SearchDrawerProps) {
    const [searchValue, setSearchValue] = useState<string>("")


    const searchForUsers = useCallback(() => {
        console.log("Searching for:", searchValue);
    }, [searchValue])

    const handleBlurSearch = () => {
        if ( searchValue ){
            searchForUsers();
        }
    }

    useEffect(() => {
        if (!searchValue) return; 

        const delayDebounceFn = setTimeout(() => {
            searchForUsers();
        }, 800);

        return () => clearTimeout(delayDebounceFn);
        
    }, [searchValue, searchForUsers]);

    return (
        <div className={`search-drawer ${isOpen ? 'open' : ''}`}>
            <div className="drawer-header">
                <h2>Search</h2>
                <div className="search-input-container">
                    <input
                        type="text" 
                        placeholder="Search..." 
                        value={searchValue}
                        onBlur={handleBlurSearch}
                        onChange={(e) => {setSearchValue(e.target.value)}}
                    />
                    <button 
                        className="clear-btn" 
                        onClick={() => {setSearchValue("")}}
                    >
                        <FontAwesomeIcon icon={faCircleXmark} />
                    </button>
                </div>
            </div>

            <div className="drawer-content">
                {/* <div className="recent-header">
                    <h4>Recent</h4>
                    <button className="clear-all">Clear all</button>
                </div> */}

                {/* Mock Recent Items */}
                {/* <ul className="recent-list">
                    <li className="recent-item">
                        <img src="/assets/img/no_user.png" alt="user" />
                        <div className="user-info">
                            <span className="username">vladdinu1</span>
                            <span className="subtext">Following</span>
                        </div>
                        <button className="remove-recent">
                            <FontAwesomeIcon icon={faXmark} />
                        </button>
                    </li>
                    <li className="recent-item">
                        <img src="/assets/img/no_user.png" alt="user" />
                        <div className="user-info">
                            <span className="username">f1_official</span>
                            <span className="subtext">Formula 1</span>
                        </div>
                        <button className="remove-recent">
                            <FontAwesomeIcon icon={faXmark} />
                        </button>
                    </li>
                </ul> */}
            </div>
        </div>
    );
}

export default SearchDrawer;