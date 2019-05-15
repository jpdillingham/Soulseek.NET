import React from 'react';

import { 
    Segment, 
    Input 
} from 'semantic-ui-react';

const SearchBox = ({ pending, onPhraseChange, onSearch }) => (
    <Segment className='search-segment'>
        <Input 
            loading={pending}
            disabled={pending}
            className='search-input'
            placeholder="Enter search phrase..."
            onChange={onPhraseChange}
            action={!pending && { content: 'Search', onClick: onSearch }}
        />
    </Segment>
);

export default SearchBox;